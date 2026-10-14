using IMS.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly string _connectionString;

        public ReportRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<ReportDashboardDto> GetReportDashboardDataAsync()
        {
            var data = new ReportDashboardDto();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // 1. Inventory Report Section
                using (var cmd = new SqlCommand(@"
                    SELECT 
                        TotalInventoryValue, TotalActiveProducts, TotalLowStockAlerts
                    FROM vw_DashboardSummary;
                    SELECT 
                        SUM(QCHoldQty) as TotalQCHold, SUM(DamagedQty) as TotalDamaged
                    FROM InventoryStocks;", connection))
                {
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        data.Inventory.TotalInventoryValue = Convert.ToDecimal(reader.GetValue(0));
                        data.Inventory.TotalActiveProducts = reader.GetInt32(1);
                        data.Inventory.LowStockCount = reader.GetInt32(2);
                    }
                    await reader.NextResultAsync();
                    if (await reader.ReadAsync())
                    {
                        data.Inventory.TotalQCHoldQty = reader.IsDBNull(0) ? 0m : Convert.ToDecimal(reader.GetValue(0));
                        data.Inventory.TotalDamagedQty = reader.IsDBNull(1) ? 0m : Convert.ToDecimal(reader.GetValue(1));
                    }
                }

                // 2. Warehouse Report Section
                using (var cmd = new SqlCommand(@"
                    SELECT 
                        COUNT(CASE WHEN UtilizationPercentage >= 80 THEN 1 END),
                        COUNT(CASE WHEN ShelfType = 'QC Hold' THEN 1 END),
                        COUNT(CASE WHEN CurrentTotalQty = 0 THEN 1 END),
                        AVG(UtilizationPercentage)
                    FROM vw_ShelfUtilization;", connection))
                {
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        data.Warehouse.CapacityWarningCount = reader.GetInt32(0);
                        data.Warehouse.QCHoldShelfCount = reader.GetInt32(1);
                        data.Warehouse.EmptyShelfCount = reader.GetInt32(2);
                        data.Warehouse.AvgUtilization = reader.IsDBNull(3) ? 0m : Convert.ToDecimal(reader.GetValue(3));
                    }
                }

                // 3. Purchase Report Section
                using (var cmd = new SqlCommand(@"
                    SELECT COUNT(*), Status FROM PurchaseOrders GROUP BY Status;", connection))
                {
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        int count = reader.GetInt32(0);
                        string status = reader.GetString(1);
                        data.Purchase.TotalOrders += count;
                        data.Purchase.StatusCounts[status] = count;
                        if (status != "Closed" && status != "Cancelled") data.Purchase.PendingOrders += count;
                    }
                }

                // 4. Sales Report Section
                using (var cmd = new SqlCommand(@"
                    SELECT COUNT(*), Status FROM SalesOrders GROUP BY Status;
                    SELECT COUNT(*) FROM Dispatches WHERE CAST(DispatchDate AS DATE) = CAST(GETDATE() AS DATE);", connection))
                {
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        int count = reader.GetInt32(0);
                        string status = reader.GetString(1);
                        data.Sales.TotalOrders += count;
                        data.Sales.StatusCounts[status] = count;
                        if (status != "Fulfilled" && status != "Cancelled") data.Sales.PendingOrders += count;
                    }
                    await reader.NextResultAsync();
                    if (await reader.ReadAsync())
                    {
                        data.Sales.DailyShipmentCount = reader.GetInt32(0);
                    }
                }

                // 5. Movement Report Section
                using (var cmd = new SqlCommand(@"
                    SELECT 
                        SUM(CASE WHEN MovementType IN ('StockIn', 'ReturnIn', 'TransferIn', 'AdjustmentIn', 'ProductionReturn') THEN Quantity ELSE 0 END),
                        SUM(CASE WHEN MovementType IN ('StockOut', 'Dispatch', 'TransferOut', 'AdjustmentOut', 'DamageOut', 'ProductionIssue') THEN ABS(Quantity) ELSE 0 END),
                        COUNT(CASE WHEN MovementType LIKE '%Transfer%' THEN 1 END),
                        COUNT(CASE WHEN MovementType LIKE '%Adjustment%' THEN 1 END),
                        COUNT(CASE WHEN MovementType = 'ProductionIssue' THEN 1 END),
                        COUNT(CASE WHEN MovementType = 'ProductionReturn' THEN 1 END)
                    FROM StockMovements WHERE CAST(CreatedAt AS DATE) >= CAST(DATEADD(day, -30, GETDATE()) AS DATE);", connection))
                {
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        data.Movement.TotalStockIn = reader.IsDBNull(0) ? 0 : reader.GetDecimal(0);
                        data.Movement.TotalStockOut = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);
                        data.Movement.TransferCount = reader.GetInt32(2);
                        data.Movement.AdjustmentCount = reader.GetInt32(3);
                        data.Movement.ProductionIssueCount = reader.GetInt32(4);
                        data.Movement.ProductionReturnCount = reader.GetInt32(5);
                    }
                }

                // 6. Production Report Section
                using (var cmd = new SqlCommand(@"
                    SELECT 
                        COUNT(*),
                        COUNT(CASE WHEN Status = 'In Production' THEN 1 END),
                        COUNT(CASE WHEN Status = 'Material Check' THEN 1 END),
                        COUNT(CASE WHEN Status = 'Completed' THEN 1 END)
                    FROM ProductionPlans;
                    SELECT 
                        SUM(AcceptedQty), SUM(RejectedQty)
                    FROM FinishedGoodsReceipts;", connection))
                {
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        data.Production.TotalPlans = reader.GetInt32(0);
                        data.Production.InProductionCount = reader.GetInt32(1);
                        data.Production.PendingMaterialCheckCount = reader.GetInt32(2);
                        data.Production.CompletedPlansCount = reader.GetInt32(3);
                    }
                    await reader.NextResultAsync();
                    if (await reader.ReadAsync())
                    {
                        data.Production.TotalAcceptedFGQty = reader.IsDBNull(0) ? 0 : reader.GetDecimal(0);
                        data.Production.TotalRejectedFGQty = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching report data.", ex);
            }

            return data;
        }

        public async Task<ReportTableDataDto> GetReportDataByKeyAsync(string reportKey)
        {
            var key = (reportKey ?? "").Trim().ToLower();
            var result = new ReportTableDataDto { ReportKey = key };

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            try
            {
                if (key.Contains("current") || key == "stock" || key == "inventory")
                {
                    result.Title = "Current Stock Report";
                    result.Category = "Inventory";
                    result.Description = "Live snapshot of stock levels and availability by shelf location.";
                    result.Headers = new List<string> { "SKU", "Product Name", "Category", "Warehouse", "Shelf Code", "Current Qty", "Reserved Qty", "Available Qty", "Unit" };

                    using var cmd = new SqlCommand(@"
                        SELECT ISNULL(p.SKU, '-') AS SKU, 
                               ISNULL(p.ProductName, 'Unknown') AS ProductName, 
                               ISNULL(c.CategoryName, 'General') AS Category,
                               ISNULL(w.WarehouseName, 'Main') AS WarehouseName, 
                               ISNULL(s.ShelfCode, '-') AS ShelfCode,
                               ISNULL(stk.CurrentQty, 0) AS CurrentQty,
                               ISNULL(stk.ReservedQty, 0) AS ReservedQty,
                               ISNULL(stk.CurrentQty - stk.ReservedQty - stk.QCHoldQty - stk.DamagedQty, 0) AS AvailableQty,
                               ISNULL(u.UnitCode, 'PCS') AS UnitCode
                        FROM Products p
                        LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                        LEFT JOIN Units u ON p.UnitID = u.UnitID
                        LEFT JOIN InventoryStocks stk ON p.ProductID = stk.ProductID
                        LEFT JOIN Shelves s ON stk.ShelfID = s.ShelfID
                        LEFT JOIN Racks rk ON s.RackID = rk.RackID
                        LEFT JOIN Rooms rm ON rk.RoomID = rm.RoomID
                        LEFT JOIN Floors f ON rm.FloorID = f.FloorID
                        LEFT JOIN Warehouses w ON f.WarehouseID = w.WarehouseID
                        WHERE p.Status = 'Active'
                        ORDER BY p.ProductName, s.ShelfCode", connection);

                    using var reader = await cmd.ExecuteReaderAsync();
                    decimal totalAvailable = 0;
                    while (await reader.ReadAsync())
                    {
                        decimal avail = reader.IsDBNull(7) ? 0 : Convert.ToDecimal(reader.GetValue(7));
                        totalAvailable += avail;
                        result.Rows.Add(new List<string>
                        {
                            reader.GetString(0),
                            reader.GetString(1),
                            reader.GetString(2),
                            reader.GetString(3),
                            reader.GetString(4),
                            $"{Convert.ToDecimal(reader.GetValue(5)):N2}",
                            $"{Convert.ToDecimal(reader.GetValue(6)):N2}",
                            $"{avail:N2}",
                            reader.GetString(8)
                        });
                    }
                    result.SummaryMetrics["Total Locations"] = result.Rows.Count.ToString();
                    result.SummaryMetrics["Total Available Units"] = $"{totalAvailable:N0}";
                }
                else if (key.Contains("low") || key.Contains("reorder"))
                {
                    result.Title = "Low Stock Report";
                    result.Category = "Inventory";
                    result.Description = "Items where available inventory has fallen below the reorder point.";
                    result.Headers = new List<string> { "SKU", "Product Name", "Category", "Total Stock", "Available", "Reorder Level", "Deficit", "Unit" };

                    using var cmd = new SqlCommand(@"
                        SELECT p.SKU, p.ProductName, ISNULL(c.CategoryName, 'General') AS Category,
                               ISNULL(SUM(stk.CurrentQty), 0) AS TotalStock,
                               ISNULL(SUM(stk.CurrentQty - stk.ReservedQty - stk.QCHoldQty - stk.DamagedQty), 0) AS AvailableStock,
                               ISNULL(p.ReorderLevel, 0) AS ReorderLevel,
                               (ISNULL(p.ReorderLevel, 0) - ISNULL(SUM(stk.CurrentQty - stk.ReservedQty - stk.QCHoldQty - stk.DamagedQty), 0)) AS Deficit,
                               ISNULL(u.UnitCode, 'PCS') AS UnitCode
                        FROM Products p
                        LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                        LEFT JOIN Units u ON p.UnitID = u.UnitID
                        LEFT JOIN InventoryStocks stk ON p.ProductID = stk.ProductID
                        WHERE p.Status = 'Active'
                        GROUP BY p.ProductID, p.SKU, p.ProductName, c.CategoryName, p.ReorderLevel, u.UnitCode
                        HAVING ISNULL(SUM(stk.CurrentQty - stk.ReservedQty - stk.QCHoldQty - stk.DamagedQty), 0) <= ISNULL(p.ReorderLevel, 0)
                        ORDER BY Deficit DESC", connection);

                    using var reader = await cmd.ExecuteReaderAsync();
                    decimal totalDeficit = 0;
                    while (await reader.ReadAsync())
                    {
                        decimal def = Convert.ToDecimal(reader.GetValue(6));
                        if (def > 0) totalDeficit += def;
                        result.Rows.Add(new List<string>
                        {
                            reader.GetString(0),
                            reader.GetString(1),
                            reader.GetString(2),
                            $"{Convert.ToDecimal(reader.GetValue(3)):N2}",
                            $"{Convert.ToDecimal(reader.GetValue(4)):N2}",
                            $"{Convert.ToDecimal(reader.GetValue(5)):N2}",
                            def > 0 ? $"+{def:N2}" : "0.00",
                            reader.GetString(7)
                        });
                    }
                    result.SummaryMetrics["Items Below Threshold"] = result.Rows.Count.ToString();
                    result.SummaryMetrics["Total Replenish Need"] = $"{totalDeficit:N0} units";
                }
                else if (key.Contains("utilization") || key.Contains("warehouse"))
                {
                    result.Title = "Warehouse Utilization Report";
                    result.Category = "Warehouse";
                    result.Description = "Space occupancy, shelf capacity warnings, and physical storage utilization.";
                    result.Headers = new List<string> { "Warehouse", "Floor", "Rack", "Shelf Code", "Type", "Max Cap", "Current Qty", "Utilization %", "Status" };

                    using var cmd = new SqlCommand(@"
                        SELECT ISNULL(w.WarehouseName, 'Main') AS WarehouseName, 
                               ISNULL(f.FloorName, 'Floor 1') AS FloorName, 
                               ISNULL(rk.RackName, '-') AS RackName, 
                               s.ShelfCode, 
                               ISNULL(s.ShelfType, 'General') AS ShelfType,
                               ISNULL(s.MaxQty, 100) AS MaxQty,
                               ISNULL(SUM(stk.CurrentQty), 0) AS CurrentQty,
                               CAST(CASE WHEN ISNULL(s.MaxQty, 0) > 0 THEN (ISNULL(SUM(stk.CurrentQty), 0) * 100.0 / s.MaxQty) ELSE 0 END AS DECIMAL(5,1)) AS UtilizationPct,
                               CASE WHEN ISNULL(s.MaxQty, 0) > 0 AND (ISNULL(SUM(stk.CurrentQty), 0) * 100.0 / s.MaxQty) >= 90 THEN 'Critical'
                                    WHEN ISNULL(s.MaxQty, 0) > 0 AND (ISNULL(SUM(stk.CurrentQty), 0) * 100.0 / s.MaxQty) >= 70 THEN 'Warning'
                                    ELSE 'Optimal' END AS Status
                        FROM Shelves s
                        LEFT JOIN Racks rk ON s.RackID = rk.RackID
                        LEFT JOIN Rooms rm ON rk.RoomID = rm.RoomID
                        LEFT JOIN Floors f ON rm.FloorID = f.FloorID
                        LEFT JOIN Warehouses w ON f.WarehouseID = w.WarehouseID
                        LEFT JOIN InventoryStocks stk ON s.ShelfID = stk.ShelfID
                        WHERE s.IsActive = 1
                        GROUP BY s.ShelfID, s.ShelfCode, s.ShelfType, s.MaxQty, w.WarehouseName, f.FloorName, rk.RackName
                        ORDER BY UtilizationPct DESC", connection);

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        result.Rows.Add(new List<string>
                        {
                            reader.GetString(0),
                            reader.GetString(1),
                            reader.GetString(2),
                            reader.GetString(3),
                            reader.GetString(4),
                            $"{Convert.ToDecimal(reader.GetValue(5)):N0}",
                            $"{Convert.ToDecimal(reader.GetValue(6)):N2}",
                            $"{Convert.ToDecimal(reader.GetValue(7)):N1}%",
                            reader.GetString(8)
                        });
                    }
                    result.SummaryMetrics["Total Monitored Shelves"] = result.Rows.Count.ToString();
                }
                else if (key.Contains("purchase") || key.Contains("po"))
                {
                    result.Title = "Purchase Order Report";
                    result.Category = "Purchase";
                    result.Description = "Procurement orders, supplier fulfillment, order status, and total order spend.";
                    result.Headers = new List<string> { "PO Number", "Supplier", "Order Date", "Expected Date", "Total Amount", "Order Status", "Created By" };

                    using var cmd = new SqlCommand(@"
                        SELECT po.PONumber, 
                               ISNULL(s.SupplierName, 'Unknown') AS Supplier,
                               CONVERT(VARCHAR(10), po.OrderDate, 120) AS OrderDate,
                               ISNULL(CONVERT(VARCHAR(10), po.ExpectedDate, 120), '-') AS ExpectedDate,
                               po.TotalAmount, 
                               po.Status, 
                               ISNULL(u.FullName, 'Purchaser') AS CreatedByName
                        FROM PurchaseOrders po
                        LEFT JOIN Suppliers s ON po.SupplierID = s.SupplierID
                        LEFT JOIN Users u ON po.CreatedBy = u.UserID
                        ORDER BY po.OrderDate DESC, po.POID DESC", connection);

                    using var reader = await cmd.ExecuteReaderAsync();
                    decimal totalSpend = 0;
                    while (await reader.ReadAsync())
                    {
                        decimal amt = Convert.ToDecimal(reader.GetValue(4));
                        totalSpend += amt;
                        result.Rows.Add(new List<string>
                        {
                            reader.GetString(0),
                            reader.GetString(1),
                            reader.GetString(2),
                            reader.GetString(3),
                            $"Rs. {amt:N2}",
                            reader.GetString(5),
                            reader.GetString(6)
                        });
                    }
                    result.SummaryMetrics["Total Orders"] = result.Rows.Count.ToString();
                    result.SummaryMetrics["Total Spend Volume"] = $"Rs. {totalSpend:N0}";
                }
                else if (key.Contains("sales") || key.Contains("so"))
                {
                    result.Title = "Sales Order Report";
                    result.Category = "Sales";
                    result.Description = "Customer order fulfillment, booking dates, revenue, and dispatch progression.";
                    result.Headers = new List<string> { "SO Number", "Customer Name", "Order Date", "Required Date", "Total Amount", "Status", "Sales Rep" };

                    using var cmd = new SqlCommand(@"
                        SELECT so.SONumber, 
                               ISNULL(c.CustomerName, 'Walk-in') AS Customer,
                               CONVERT(VARCHAR(10), so.OrderDate, 120) AS OrderDate,
                               ISNULL(CONVERT(VARCHAR(10), so.RequiredDate, 120), '-') AS DeliveryDate,
                               so.TotalAmount, 
                               so.Status,
                               ISNULL(u.FullName, 'Sales Rep') AS CreatedBy
                        FROM SalesOrders so
                        LEFT JOIN Customers c ON so.CustomerID = c.CustomerID
                        LEFT JOIN Users u ON so.CreatedBy = u.UserID
                        ORDER BY so.OrderDate DESC, so.SOID DESC", connection);

                    using var reader = await cmd.ExecuteReaderAsync();
                    decimal totalRevenue = 0;
                    while (await reader.ReadAsync())
                    {
                        decimal amt = Convert.ToDecimal(reader.GetValue(4));
                        totalRevenue += amt;
                        result.Rows.Add(new List<string>
                        {
                            reader.GetString(0),
                            reader.GetString(1),
                            reader.GetString(2),
                            reader.GetString(3),
                            $"Rs. {amt:N2}",
                            reader.GetString(5),
                            reader.GetString(6)
                        });
                    }
                    result.SummaryMetrics["Total Orders"] = result.Rows.Count.ToString();
                    result.SummaryMetrics["Total Revenue"] = $"Rs. {totalRevenue:N0}";
                }
                else if (key.Contains("movement") || key.Contains("audit-trail"))
                {
                    result.Title = "Stock Movement Report";
                    result.Category = "Inventory";
                    result.Description = "Comprehensive ledger of stock inflows, dispatches, adjustments, and internal transfers.";
                    result.Headers = new List<string> { "Timestamp", "SKU", "Product Name", "Movement Type", "Quantity", "Reference #", "User" };

                    using var cmd = new SqlCommand(@"
                        SELECT TOP 200
                               CONVERT(VARCHAR(16), sm.CreatedAt, 120) AS MovementDate,
                               ISNULL(p.SKU, '-') AS SKU, 
                               ISNULL(p.ProductName, 'Unknown') AS ProductName,
                               sm.MovementType,
                               sm.Quantity,
                               ISNULL(sm.ReferenceNo, '-') AS Reference,
                               ISNULL(u.FullName, 'System') AS PerformedBy
                        FROM StockMovements sm
                        LEFT JOIN Products p ON sm.ProductID = p.ProductID
                        LEFT JOIN Users u ON sm.PerformedBy = u.UserID
                        ORDER BY sm.CreatedAt DESC, sm.MovementID DESC", connection);

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        decimal qty = Convert.ToDecimal(reader.GetValue(4));
                        result.Rows.Add(new List<string>
                        {
                            reader.GetString(0),
                            reader.GetString(1),
                            reader.GetString(2),
                            reader.GetString(3),
                            qty > 0 ? $"+{qty:N2}" : $"{qty:N2}",
                            reader.GetString(5),
                            reader.GetString(6)
                        });
                    }
                    result.SummaryMetrics["Recent Transactions"] = result.Rows.Count.ToString();
                }
                else // Valuation / Default
                {
                    result.Title = "Inventory Valuation Report";
                    result.Category = "Inventory";
                    result.Description = "Asset accounting valuation comparing total stock units at standard cost versus retail value.";
                    result.Headers = new List<string> { "SKU", "Product Name", "Category", "Total Stock", "Unit Cost", "Selling Price", "Total Asset Value", "Potential Revenue" };

                    using var cmd = new SqlCommand(@"
                        SELECT p.SKU, 
                               p.ProductName, 
                               ISNULL(c.CategoryName, 'General') AS Category,
                               ISNULL(SUM(stk.CurrentQty), 0) AS TotalStock,
                               p.StandardCost,
                               p.SellingPrice,
                               (ISNULL(SUM(stk.CurrentQty), 0) * ISNULL(p.StandardCost, 0)) AS TotalAssetValue,
                               (ISNULL(SUM(stk.CurrentQty), 0) * ISNULL(p.SellingPrice, 0)) AS PotentialRevenue
                        FROM Products p
                        LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                        LEFT JOIN InventoryStocks stk ON p.ProductID = stk.ProductID
                        WHERE p.Status = 'Active'
                        GROUP BY p.ProductID, p.SKU, p.ProductName, c.CategoryName, p.StandardCost, p.SellingPrice
                        ORDER BY TotalAssetValue DESC", connection);

                    using var reader = await cmd.ExecuteReaderAsync();
                    decimal totalAsset = 0;
                    decimal totalSales = 0;
                    while (await reader.ReadAsync())
                    {
                        decimal asset = Convert.ToDecimal(reader.GetValue(6));
                        decimal sales = Convert.ToDecimal(reader.GetValue(7));
                        totalAsset += asset;
                        totalSales += sales;
                        result.Rows.Add(new List<string>
                        {
                            reader.GetString(0),
                            reader.GetString(1),
                            reader.GetString(2),
                            $"{Convert.ToDecimal(reader.GetValue(3)):N2}",
                            $"Rs. {Convert.ToDecimal(reader.GetValue(4)):N2}",
                            $"Rs. {Convert.ToDecimal(reader.GetValue(5)):N2}",
                            $"Rs. {asset:N2}",
                            $"Rs. {sales:N2}"
                        });
                    }
                    result.SummaryMetrics["Total Inventory Asset Value"] = $"Rs. {totalAsset:N0}";
                    result.SummaryMetrics["Projected Retail Value"] = $"Rs. {totalSales:N0}";
                }

                result.TotalCount = result.Rows.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetReportDataByKeyAsync: " + ex.Message);
            }

            return result;
        }

        public async Task<IEnumerable<ReportScheduleDto>> GetReportSchedulesAsync()
        {
            await Task.Yield();
            return new List<ReportScheduleDto>
            {
                new ReportScheduleDto { ScheduleID = 1, ReportName = "Current Stock Report", Frequency = "Daily", Time = "08:00 AM", Recipients = "manager@ims.com, inventory@ims.com", Format = "PDF, Excel", IsActive = true, LastSent = "Today 08:00 AM", NextRun = "Tomorrow, 08:00 AM" },
                new ReportScheduleDto { ScheduleID = 2, ReportName = "Low Stock Report", Frequency = "Daily", Time = "09:00 AM", Recipients = "procurement@ims.com", Format = "PDF, Excel", IsActive = true, LastSent = "Today 09:00 AM", NextRun = "Tomorrow, 09:00 AM" },
                new ReportScheduleDto { ScheduleID = 3, ReportName = "Warehouse Utilization Report", Frequency = "Weekly (Mondays)", Time = "07:30 AM", Recipients = "logistics@ims.com", Format = "PDF", IsActive = true, LastSent = "Last Monday", NextRun = "Next Monday, 07:30 AM" },
                new ReportScheduleDto { ScheduleID = 4, ReportName = "Inventory Valuation Report", Frequency = "Monthly (1st)", Time = "06:00 AM", Recipients = "finance@ims.com, admin@ims.com", Format = "PDF, Excel", IsActive = true, LastSent = "1st of Month", NextRun = "1st of next month" }
            };
        }
    }
}

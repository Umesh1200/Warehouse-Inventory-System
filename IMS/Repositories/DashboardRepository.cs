using IMS.Models.DTOs;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IMS.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly string _connectionString;

        public DashboardRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(int? warehouseId = null)
        {
            var summary = new DashboardSummaryDto();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = warehouseId.HasValue
                    ? new SqlCommand(@"
                        SELECT
                            (SELECT COUNT(DISTINCT p.ProductID)
                             FROM Products p
                             INNER JOIN InventoryStocks inv ON p.ProductID = inv.ProductID
                             INNER JOIN Shelves s ON inv.ShelfID = s.ShelfID
                             INNER JOIN Racks r ON s.RackID = r.RackID
                             INNER JOIN Rooms rm ON r.RoomID = rm.RoomID
                             INNER JOIN Floors f ON rm.FloorID = f.FloorID
                             WHERE p.Status = 'Active' AND f.WarehouseID = @WarehouseID) AS TotalActiveProducts,
                            (SELECT COUNT(*)
                             FROM dbo.vw_LowStockItems l
                             INNER JOIN Warehouses w ON l.WarehouseName = w.WarehouseName
                             WHERE w.WarehouseID = @WarehouseID) AS TotalLowStockAlerts,
                            (SELECT ISNULL(SUM(inv.CurrentQty * p.StandardCost), 0)
                             FROM InventoryStocks inv
                             INNER JOIN Products p ON inv.ProductID = p.ProductID
                             INNER JOIN Shelves s ON inv.ShelfID = s.ShelfID
                             INNER JOIN Racks r ON s.RackID = r.RackID
                             INNER JOIN Rooms rm ON r.RoomID = rm.RoomID
                             INNER JOIN Floors f ON rm.FloorID = f.FloorID
                             WHERE f.WarehouseID = @WarehouseID) AS TotalInventoryValue,
                            (SELECT COUNT(*) FROM PurchaseOrders WHERE Status IN ('Draft', 'Confirmed', 'InTransit')) AS PendingPurchaseOrders,
                            (SELECT COUNT(*) FROM SalesOrders WHERE Status IN ('Confirmed', 'Allocated', 'ReadyToPick', 'Picking', 'Packed')) AS PendingSalesOrders,
                            (SELECT COUNT(*) FROM Dispatches WHERE CAST(DispatchDate AS DATE) = CAST(GETDATE() AS DATE)) AS DailyShipments;", connection)
                    : new SqlCommand("SELECT * FROM dbo.vw_DashboardSummary", connection);

                if (warehouseId.HasValue)
                {
                    command.Parameters.AddWithValue("@WarehouseID", warehouseId.Value);
                }

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    summary.TotalActiveProducts = reader.GetInt32("TotalActiveProducts");
                    summary.TotalLowStockAlerts = reader.GetInt32("TotalLowStockAlerts");
                    summary.TotalInventoryValue = reader.GetDecimal("TotalInventoryValue");
                    summary.PendingPurchaseOrders = reader.GetInt32("PendingPurchaseOrders");
                    summary.PendingSalesOrders = reader.GetInt32("PendingSalesOrders");
                    summary.DailyShipments = reader.GetInt32("DailyShipments");
                }

                await reader.DisposeAsync();

                using var shipmentCommand = new SqlCommand(@"
                    SELECT COUNT(DISTINCT d.DispatchID)
                    FROM Dispatches d
                    INNER JOIN SalesOrders so ON d.SOID = so.SOID
                    WHERE CAST(d.DispatchDate AS DATE) = CAST(GETDATE() AS DATE)
                        AND ISNULL(d.Status, '') <> 'Failed'
                        AND (
                            @WarehouseID IS NULL
                            OR EXISTS (
                                SELECT 1
                                FROM StockReservations sr
                                INNER JOIN Shelves s ON sr.ShelfID = s.ShelfID
                                INNER JOIN Racks r ON s.RackID = r.RackID
                                INNER JOIN Rooms rm ON r.RoomID = rm.RoomID
                                INNER JOIN Floors f ON rm.FloorID = f.FloorID
                                WHERE sr.ReferenceType = 'SalesOrder'
                                    AND sr.ReferenceNo = so.SONumber
                                    AND f.WarehouseID = @WarehouseID
                            )
                        );", connection);
                shipmentCommand.Parameters.AddWithValue("@WarehouseID", warehouseId.HasValue ? warehouseId.Value : DBNull.Value);
                summary.DailyShipments = Convert.ToInt32(await shipmentCommand.ExecuteScalarAsync() ?? 0);
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching dashboard summary.", ex);
            }

            return summary;
        }

        public async Task<IEnumerable<DashboardLowStockDto>> GetLowStockItemsAsync(int limit = 10, int? warehouseId = null)
        {
            var results = new List<DashboardLowStockDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand($@"
                    SELECT TOP {limit} l.*
                    FROM dbo.vw_LowStockItems l
                    INNER JOIN Warehouses w ON l.WarehouseName = w.WarehouseName
                    WHERE @WarehouseID IS NULL OR w.WarehouseID = @WarehouseID
                    ORDER BY l.AvailableQty ASC", connection);
                command.Parameters.AddWithValue("@WarehouseID", warehouseId.HasValue ? warehouseId.Value : DBNull.Value);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new DashboardLowStockDto
                    {
                        ProductID = reader.GetInt32("ProductID"),
                        SKU = reader.GetString("SKU"),
                        ProductName = reader.GetString("ProductName"),
                        CategoryName = reader.IsDBNull("CategoryName") ? "" : reader.GetString("CategoryName"),
                        WarehouseName = reader.IsDBNull("WarehouseName") ? "" : reader.GetString("WarehouseName"),
                        ShelfCode = reader.IsDBNull("ShelfCode") ? "" : reader.GetString("ShelfCode"),
                        AvailableQty = reader.GetDecimal("AvailableQty"),
                        ReorderLevel = reader.GetDecimal("ReorderLevel"),
                        StockStatus = reader.GetString("StockStatus")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching low stock items.", ex);
            }

            return results;
        }

        public async Task<IEnumerable<DashboardShelfUtilizationDto>> GetShelfUtilizationAsync(int limit = 10, int? warehouseId = null)
        {
            var results = new List<DashboardShelfUtilizationDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand($@"
                    SELECT TOP {limit} u.*
                    FROM dbo.vw_ShelfUtilization u
                    INNER JOIN Warehouses w ON u.WarehouseName = w.WarehouseName
                    WHERE @WarehouseID IS NULL OR w.WarehouseID = @WarehouseID
                    ORDER BY u.UtilizationPercentage DESC", connection);
                command.Parameters.AddWithValue("@WarehouseID", warehouseId.HasValue ? warehouseId.Value : DBNull.Value);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new DashboardShelfUtilizationDto
                    {
                        WarehouseName = reader.GetString("WarehouseName"),
                        FloorName = reader.IsDBNull("FloorName") ? "" : reader.GetString("FloorName"),
                        RoomName = reader.IsDBNull("RoomName") ? "" : reader.GetString("RoomName"),
                        RackName = reader.IsDBNull("RackName") ? "" : reader.GetString("RackName"),
                        ShelfID = reader.GetInt32("ShelfID"),
                        ShelfCode = reader.GetString("ShelfCode"),
                        ShelfType = reader.IsDBNull("ShelfType") ? "" : reader.GetString("ShelfType"),
                        MaxQty = reader.GetDecimal("MaxQty"),
                        CurrentTotalQty = reader.GetDecimal("CurrentTotalQty"),
                        UtilizationPercentage = reader.GetDecimal("UtilizationPercentage")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching shelf utilization.", ex);
            }

            return results;
        }

        public async Task<IEnumerable<DashboardMovementDto>> GetRecentMovementsAsync(int limit = 10, int? warehouseId = null)
        {
            var results = new List<DashboardMovementDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand($@"
                    SELECT TOP {limit} m.*
                    FROM dbo.vw_StockMovementReport m
                    INNER JOIN Warehouses w ON m.WarehouseName = w.WarehouseName
                    WHERE @WarehouseID IS NULL OR w.WarehouseID = @WarehouseID
                    ORDER BY m.MovementDate DESC", connection);
                command.Parameters.AddWithValue("@WarehouseID", warehouseId.HasValue ? warehouseId.Value : DBNull.Value);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new DashboardMovementDto
                    {
                        MovementID = reader.GetInt32("MovementID"),
                        MovementDate = reader.GetDateTime("MovementDate"),
                        SKU = reader.GetString("SKU"),
                        ProductName = reader.GetString("ProductName"),
                        WarehouseName = reader.GetString("WarehouseName"),
                        ShelfCode = reader.GetString("ShelfCode"),
                        MovementType = reader.GetString("MovementType"),
                        Quantity = reader.GetDecimal("Quantity"),
                        BeforeQty = reader.GetDecimal("BeforeQty"),
                        AfterQty = reader.GetDecimal("AfterQty"),
                        ReferenceType = reader.IsDBNull("ReferenceType") ? "" : reader.GetString("ReferenceType"),
                        ReferenceNo = reader.IsDBNull("ReferenceNo") ? "" : reader.GetString("ReferenceNo"),
                        PerformedBy = reader.IsDBNull("PerformedBy") ? "" : reader.GetString("PerformedBy"),
                        Remarks = reader.IsDBNull("Remarks") ? "" : reader.GetString("Remarks")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching recent movements.", ex);
            }

            return results;
        }

        public async Task<IEnumerable<DashboardWarehouseOptionDto>> GetWarehousesAsync()
        {
            var results = new List<DashboardWarehouseOptionDto>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT WarehouseID, WarehouseName
                FROM Warehouses
                WHERE IsActive = 1
                ORDER BY WarehouseName", connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                results.Add(new DashboardWarehouseOptionDto
                {
                    WarehouseID = reader.GetInt32("WarehouseID"),
                    WarehouseName = reader.GetString("WarehouseName")
                });
            }

            return results;
        }

        public async Task<IEnumerable<DashboardStockValueDto>> GetStockValueBreakdownAsync(int limit = 8, int? warehouseId = null)
        {
            var results = new List<DashboardStockValueDto>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand($@"
                SELECT TOP {limit}
                    p.SKU,
                    p.ProductName,
                    ISNULL(SUM(inv.CurrentQty * p.StandardCost), 0) AS StockValue
                FROM InventoryStocks inv
                INNER JOIN Products p ON inv.ProductID = p.ProductID
                INNER JOIN Shelves s ON inv.ShelfID = s.ShelfID
                INNER JOIN Racks r ON s.RackID = r.RackID
                INNER JOIN Rooms rm ON r.RoomID = rm.RoomID
                INNER JOIN Floors f ON rm.FloorID = f.FloorID
                WHERE p.Status = 'Active'
                    AND (@WarehouseID IS NULL OR f.WarehouseID = @WarehouseID)
                GROUP BY p.SKU, p.ProductName
                HAVING ISNULL(SUM(inv.CurrentQty * p.StandardCost), 0) > 0
                ORDER BY StockValue DESC", connection);
            command.Parameters.AddWithValue("@WarehouseID", warehouseId.HasValue ? warehouseId.Value : DBNull.Value);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                results.Add(new DashboardStockValueDto
                {
                    SKU = reader.GetString("SKU"),
                    ProductName = reader.GetString("ProductName"),
                    StockValue = reader.GetDecimal("StockValue")
                });
            }

            return results;
        }

        public async Task<IEnumerable<DashboardStockValueDto>> GetStockValueByCategoryAsync(int? warehouseId = null)
        {
            var results = new List<DashboardStockValueDto>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT
                    c.CategoryName,
                    ISNULL(SUM(inv.CurrentQty * p.StandardCost), 0) AS StockValue
                FROM InventoryStocks inv
                INNER JOIN Products p ON inv.ProductID = p.ProductID
                LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                INNER JOIN Shelves s ON inv.ShelfID = s.ShelfID
                INNER JOIN Racks r ON s.RackID = r.RackID
                INNER JOIN Rooms rm ON r.RoomID = rm.RoomID
                INNER JOIN Floors f ON rm.FloorID = f.FloorID
                WHERE p.Status = 'Active'
                    AND (@WarehouseID IS NULL OR f.WarehouseID = @WarehouseID)
                GROUP BY c.CategoryName
                HAVING ISNULL(SUM(inv.CurrentQty * p.StandardCost), 0) > 0
                ORDER BY StockValue DESC", connection);
            command.Parameters.AddWithValue("@WarehouseID", warehouseId.HasValue ? warehouseId.Value : DBNull.Value);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                results.Add(new DashboardStockValueDto
                {
                    SKU = string.Empty,
                    ProductName = reader.IsDBNull("CategoryName") ? "Uncategorized" : reader.GetString("CategoryName"),
                    StockValue = reader.GetDecimal("StockValue")
                });
            }

            return results;
        }

        public async Task<IEnumerable<DashboardMovementTrendDto>> GetMovementTrendAsync(int days = 7, int? warehouseId = null)
        {
            var results = new List<DashboardMovementTrendDto>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT
                    CAST(MovementDate AS DATE) AS MovementDay,
                    SUM(CASE WHEN MovementType IN ('StockIn', 'TransferIn', 'AdjustmentIn', 'ReturnIn', 'ProductionReturn') OR MovementType LIKE '%In'
                        THEN ABS(Quantity) ELSE 0 END) AS InboundQty,
                    SUM(CASE WHEN MovementType IN ('StockOut', 'Dispatch', 'TransferOut', 'AdjustmentOut', 'DamageOut', 'ProductionIssue') OR MovementType LIKE '%Out'
                        THEN ABS(Quantity) ELSE 0 END) AS OutboundQty
                FROM dbo.vw_StockMovementReport m
                INNER JOIN Warehouses w ON m.WarehouseName = w.WarehouseName
                WHERE MovementDate >= DATEADD(DAY, -(@Days - 1), CAST(GETDATE() AS DATE))
                    AND (@WarehouseID IS NULL OR w.WarehouseID = @WarehouseID)
                GROUP BY CAST(MovementDate AS DATE)
                ORDER BY MovementDay", connection);
            command.Parameters.AddWithValue("@Days", days);
            command.Parameters.AddWithValue("@WarehouseID", warehouseId.HasValue ? warehouseId.Value : DBNull.Value);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                results.Add(new DashboardMovementTrendDto
                {
                    Date = reader.GetDateTime("MovementDay"),
                    InboundQty = reader.GetDecimal("InboundQty"),
                    OutboundQty = reader.GetDecimal("OutboundQty")
                });
            }

            return results;
        }

        public async Task<IEnumerable<DashboardTopProductDto>> GetTopProductsByValueAsync(int limit = 6, int? warehouseId = null)
        {
            var results = new List<DashboardTopProductDto>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT TOP (@Limit)
                    p.ProductID,
                    p.SKU,
                    p.ProductName,
                    ISNULL(c.CategoryName, 'General') AS CategoryName,
                    p.ProductType,
                    '' AS ImagePath,
                    ISNULL(p.StandardCost, 0) AS UnitCost,
                    ISNULL(SUM(inv.CurrentQty), 0) AS AvailableQty,
                    ISNULL(SUM(inv.CurrentQty * ISNULL(p.StandardCost, 0)), 0) AS TotalValue,
                    CASE 
                        WHEN ISNULL(SUM(inv.CurrentQty), 0) <= 0 THEN 'Out of Stock'
                        WHEN ISNULL(SUM(inv.CurrentQty), 0) <= p.ReorderLevel THEN 'Low Stock'
                        ELSE 'In Stock'
                    END AS StockStatus
                FROM Products p
                INNER JOIN InventoryStocks inv ON p.ProductID = inv.ProductID
                INNER JOIN Shelves s ON inv.ShelfID = s.ShelfID
                INNER JOIN Racks r ON s.RackID = r.RackID
                INNER JOIN Rooms rm ON r.RoomID = rm.RoomID
                INNER JOIN Floors f ON rm.FloorID = f.FloorID
                LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                WHERE p.Status = 'Active'
                    AND (@WarehouseID IS NULL OR f.WarehouseID = @WarehouseID)
                GROUP BY p.ProductID, p.SKU, p.ProductName, c.CategoryName, p.ProductType, p.StandardCost, p.ReorderLevel
                ORDER BY TotalValue DESC", connection);

            command.Parameters.AddWithValue("@Limit", limit);
            command.Parameters.AddWithValue("@WarehouseID", warehouseId.HasValue ? warehouseId.Value : DBNull.Value);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new DashboardTopProductDto
                {
                    ProductID = reader.GetInt32("ProductID"),
                    SKU = reader.GetString("SKU"),
                    ProductName = reader.GetString("ProductName"),
                    CategoryName = reader.GetString("CategoryName"),
                    ProductType = reader.IsDBNull("ProductType") ? "" : reader.GetString("ProductType"),
                    ImagePath = reader.GetString("ImagePath"),
                    UnitCost = reader.GetDecimal("UnitCost"),
                    AvailableQty = reader.GetDecimal("AvailableQty"),
                    TotalValue = reader.GetDecimal("TotalValue"),
                    StockStatus = reader.GetString("StockStatus")
                });
            }
            return results;
        }

        public async Task<IEnumerable<DashboardTopProductDto>> GetTopSellingProductsAsync(int limit = 6)
        {
            var results = new List<DashboardTopProductDto>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT TOP (@Limit)
                    p.ProductID,
                    p.SKU,
                    p.ProductName,
                    ISNULL(c.CategoryName, 'General') AS CategoryName,
                    p.ProductType,
                    '' AS ImagePath,
                    ISNULL(p.StandardCost, 0) AS UnitCost,
                    ISNULL(SUM(soi.OrderedQty), 0) AS UnitsSold,
                    ISNULL(SUM(soi.OrderedQty * soi.UnitPrice), 0) AS TotalSalesAmount,
                    ISNULL((SELECT SUM(ist.CurrentQty) FROM InventoryStocks ist WHERE ist.ProductID = p.ProductID), 0) AS AvailableQty,
                    CASE 
                        WHEN ISNULL((SELECT SUM(ist.CurrentQty) FROM InventoryStocks ist WHERE ist.ProductID = p.ProductID), 0) <= 0 THEN 'Out of Stock'
                        ELSE 'In Stock'
                    END AS StockStatus
                FROM Products p
                INNER JOIN SalesOrderItems soi ON p.ProductID = soi.ProductID
                INNER JOIN SalesOrders so ON soi.SOID = so.SOID
                LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                WHERE so.Status NOT IN ('Draft', 'Cancelled')
                GROUP BY p.ProductID, p.SKU, p.ProductName, c.CategoryName, p.ProductType, p.StandardCost
                ORDER BY UnitsSold DESC", connection);

            command.Parameters.AddWithValue("@Limit", limit);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new DashboardTopProductDto
                {
                    ProductID = reader.GetInt32("ProductID"),
                    SKU = reader.GetString("SKU"),
                    ProductName = reader.GetString("ProductName"),
                    CategoryName = reader.GetString("CategoryName"),
                    ProductType = reader.IsDBNull("ProductType") ? "" : reader.GetString("ProductType"),
                    ImagePath = reader.GetString("ImagePath"),
                    UnitCost = reader.GetDecimal("UnitCost"),
                    UnitsSold = reader.GetDecimal("UnitsSold"),
                    TotalSalesAmount = reader.GetDecimal("TotalSalesAmount"),
                    AvailableQty = reader.GetDecimal("AvailableQty"),
                    StockStatus = reader.GetString("StockStatus")
                });
            }
            return results;
        }

        public async Task<DashboardProcurementFinanceDto> GetProcurementFinanceSummaryAsync()
        {
            var summary = new DashboardProcurementFinanceDto();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT
                    (SELECT ISNULL(SUM(inv.CurrentQty * p.StandardCost), 0) FROM InventoryStocks inv INNER JOIN Products p ON inv.ProductID = p.ProductID) AS TotalInventoryCapital,
                    (SELECT ISNULL(SUM(TotalAmount), 0) FROM PurchaseOrders WHERE MONTH(OrderDate) = MONTH(GETDATE()) AND YEAR(OrderDate) = YEAR(GETDATE()) AND Status <> 'Cancelled') AS MonthlyPOSpend,
                    (SELECT ISNULL(SUM(TotalAmount), 0) FROM SalesOrders WHERE MONTH(OrderDate) = MONTH(GETDATE()) AND YEAR(OrderDate) = YEAR(GETDATE()) AND Status NOT IN ('Draft', 'Cancelled')) AS MonthlySalesRevenue,
                    (SELECT ISNULL(SUM(TotalAmount), 0) FROM PurchaseOrders WHERE Status IN ('Draft', 'Confirmed', 'InTransit')) AS PendingPOCommitments,
                    (SELECT ISNULL(SUM(TotalAmount), 0) FROM SalesOrders WHERE Status IN ('Confirmed', 'Allocated', 'ReadyToPick', 'Picking', 'Packed')) AS PendingSalesReceivables,
                    (SELECT COUNT(*) FROM Suppliers WHERE Status = 'Active') AS ActiveSuppliersCount,
                    (SELECT COUNT(*) FROM Transfers WHERE Status IN ('Pending', 'Awaiting Approval')) AS PendingApprovalTransfers", connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                summary.TotalInventoryCapital = reader.GetDecimal("TotalInventoryCapital");
                summary.MonthlyPOSpend = reader.GetDecimal("MonthlyPOSpend");
                summary.MonthlySalesRevenue = reader.GetDecimal("MonthlySalesRevenue");
                summary.PendingPOCommitments = reader.GetDecimal("PendingPOCommitments");
                summary.PendingSalesReceivables = reader.GetDecimal("PendingSalesReceivables");
                summary.ActiveSuppliersCount = reader.GetInt32("ActiveSuppliersCount");
                summary.PendingApprovalTransfers = reader.GetInt32("PendingApprovalTransfers");
            }
            return summary;
        }

        public async Task<IEnumerable<DashboardTopSupplierDto>> GetTopSuppliersAsync(int limit = 5)
        {
            var results = new List<DashboardTopSupplierDto>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT TOP (@Limit)
                    s.SupplierID,
                    s.SupplierName,
                    ISNULL(s.ContactPerson, '') AS ContactPerson,
                    COUNT(DISTINCT po.POID) AS TotalOrders,
                    ISNULL(SUM(po.TotalAmount), 0) AS TotalSpend,
                    ISNULL(AVG(CASE WHEN qc.AcceptedQty + qc.RejectedQty > 0 THEN (qc.AcceptedQty * 100.0) / (qc.AcceptedQty + qc.RejectedQty) ELSE 100 END), 100) AS QCApprovalRate
                FROM Suppliers s
                LEFT JOIN PurchaseOrders po ON s.SupplierID = po.SupplierID AND po.Status <> 'Cancelled'
                LEFT JOIN PurchaseOrderItems poi ON po.POID = poi.POID
                LEFT JOIN ReceivingItems ri ON poi.POItemID = ri.POItemID
                LEFT JOIN QCChecks qc ON ri.ReceiptID = qc.ReceiptID
                WHERE s.Status = 'Active'
                GROUP BY s.SupplierID, s.SupplierName, s.ContactPerson
                ORDER BY TotalSpend DESC, TotalOrders DESC", connection);

            command.Parameters.AddWithValue("@Limit", limit);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new DashboardTopSupplierDto
                {
                    SupplierID = reader.GetInt32("SupplierID"),
                    SupplierName = reader.GetString("SupplierName"),
                    ContactPerson = reader.GetString("ContactPerson"),
                    TotalOrders = reader.GetInt32("TotalOrders"),
                    TotalSpend = reader.GetDecimal("TotalSpend"),
                    QCApprovalRate = Math.Round(reader.GetDecimal("QCApprovalRate"), 1)
                });
            }
            return results;
        }

        public async Task<IEnumerable<DashboardPendingPipelineDto>> GetInboundPipelineAsync(int limit = 5)
        {
            var results = new List<DashboardPendingPipelineDto>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT TOP (@Limit)
                    po.POID,
                    po.PONumber,
                    s.SupplierName,
                    po.OrderDate,
                    po.ExpectedDate,
                    po.TotalAmount,
                    po.Status,
                    (SELECT COUNT(*) FROM PurchaseOrderItems poi WHERE poi.POID = po.POID) AS ItemCount
                FROM PurchaseOrders po
                INNER JOIN Suppliers s ON po.SupplierID = s.SupplierID
                WHERE po.Status IN ('Confirmed', 'InTransit', 'QCCheck', 'Putaway')
                ORDER BY ISNULL(po.ExpectedDate, po.OrderDate) ASC", connection);

            command.Parameters.AddWithValue("@Limit", limit);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new DashboardPendingPipelineDto
                {
                    POID = reader.GetInt32("POID"),
                    PONumber = reader.GetString("PONumber"),
                    SupplierName = reader.GetString("SupplierName"),
                    OrderDate = reader.GetDateTime("OrderDate"),
                    ExpectedDate = reader.IsDBNull("ExpectedDate") ? (DateTime?)null : reader.GetDateTime("ExpectedDate"),
                    TotalAmount = reader.GetDecimal("TotalAmount"),
                    Status = reader.GetString("Status"),
                    ItemCount = reader.GetInt32("ItemCount")
                });
            }
            return results;
        }

        public async Task<IEnumerable<DashboardStaffPerformanceDto>> GetStaffPerformanceAsync(int limit = 6)
        {
            var results = new List<DashboardStaffPerformanceDto>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT TOP (@Limit)
                    u.UserID,
                    u.FullName,
                    ISNULL(r.RoleName, 'Warehouse Staff') AS RoleName,
                    COUNT(m.MovementID) AS TasksCompletedToday,
                    ISNULL(SUM(ABS(m.Quantity)), 0) AS UnitsMovedToday,
                    ISNULL(MAX(m.CreatedAt), GETDATE()) AS LastActiveDate
                FROM Users u
                LEFT JOIN Roles r ON u.RoleID = r.RoleID
                INNER JOIN StockMovements m ON u.UserID = m.PerformedBy AND CAST(m.CreatedAt AS DATE) = CAST(GETDATE() AS DATE)
                WHERE u.IsActive = 1
                GROUP BY u.UserID, u.FullName, r.RoleName
                ORDER BY TasksCompletedToday DESC, UnitsMovedToday DESC", connection);

            command.Parameters.AddWithValue("@Limit", limit);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string name = reader.GetString("FullName");
                var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string initials = parts.Length >= 2 ? $"{parts[0][0]}{parts[1][0]}".ToUpper() : name.Substring(0, Math.Min(2, name.Length)).ToUpper();
                int hash = name.GetHashCode();
                var colors = new[] { "#8b5cf6", "#14b8a6", "#f59e0b", "#ef4444", "#3b82f6", "#10b981", "#6366f1" };
                string avatarColor = colors[Math.Abs(hash) % colors.Length];
                DateTime lastActive = reader.GetDateTime("LastActiveDate");

                results.Add(new DashboardStaffPerformanceDto
                {
                    UserID = reader.GetInt32("UserID"),
                    FullName = name,
                    RoleName = reader.GetString("RoleName"),
                    TasksCompletedToday = reader.GetInt32("TasksCompletedToday"),
                    UnitsMovedToday = reader.GetDecimal("UnitsMovedToday"),
                    Initials = initials,
                    AvatarColor = avatarColor,
                    LastActiveTime = lastActive.ToString("hh:mm tt")
                });
            }
            return results;
        }
    }
}

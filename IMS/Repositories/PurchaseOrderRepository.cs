using IMS.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public class PurchaseOrderRepository : IPurchaseOrderRepository
    {
        private readonly string _connectionString;

        public PurchaseOrderRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<IEnumerable<PurchaseOrderDto>> GetAllPurchaseOrdersAsync()
        {
            var results = new List<PurchaseOrderDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT po.*, 
                           s.SupplierName, 
                           uc.FullName AS CreatedByName, 
                           ua.FullName AS ApprovedByName,
                           items.ItemCount,
                           items.TotalOrderedQty,
                           items.TotalReceivedQty
                    FROM PurchaseOrders po
                    INNER JOIN Suppliers s ON po.SupplierID = s.SupplierID
                    LEFT JOIN Users uc ON po.CreatedBy = uc.UserID
                    LEFT JOIN Users ua ON po.ApprovedBy = ua.UserID
                    LEFT JOIN (
                        SELECT POID, 
                               COUNT(*) AS ItemCount, 
                               SUM(OrderedQty) AS TotalOrderedQty, 
                               SUM(ReceivedQty) AS TotalReceivedQty
                        FROM PurchaseOrderItems
                        GROUP BY POID
                    ) items ON po.POID = items.POID
                    ORDER BY po.OrderDate DESC", connection);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new PurchaseOrderDto
                    {
                        POID = reader.GetInt32("POID"),
                        PONumber = reader.GetString("PONumber"),
                        SupplierID = reader.GetInt32("SupplierID"),
                        SupplierName = reader.GetString("SupplierName"),
                        OrderDate = reader.GetDateTime("OrderDate"),
                        ExpectedDate = reader.IsDBNull("ExpectedDate") ? (DateTime?)null : reader.GetDateTime("ExpectedDate"),

                        TotalAmount = reader.GetDecimal("TotalAmount"),
                        Status = reader.GetString("Status"),
                        CreatedBy = reader.GetInt32("CreatedBy"),
                        CreatedByName = reader.IsDBNull("CreatedByName") ? "" : reader.GetString("CreatedByName"),
                        ApprovedBy = reader.IsDBNull("ApprovedBy") ? (int?)null : reader.GetInt32("ApprovedBy"),
                        ApprovedByName = reader.IsDBNull("ApprovedByName") ? "" : reader.GetString("ApprovedByName"),
                        Remarks = reader.IsDBNull("Remarks") ? "" : reader.GetString("Remarks"),
                        Carrier = reader.IsDBNull("Carrier") ? "" : reader.GetString("Carrier"),
                        TrackingNumber = reader.IsDBNull("TrackingNumber") ? "" : reader.GetString("TrackingNumber"),
                        RejectionReason = reader.IsDBNull("RejectionReason") ? "" : reader.GetString("RejectionReason"),
                        ItemCount = reader.IsDBNull("ItemCount") ? 0 : reader.GetInt32("ItemCount"),
                        TotalOrderedQty = reader.IsDBNull("TotalOrderedQty") ? 0 : reader.GetDecimal("TotalOrderedQty"),
                        TotalReceivedQty = reader.IsDBNull("TotalReceivedQty") ? 0 : reader.GetDecimal("TotalReceivedQty")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching purchase orders.", ex);
            }

            return results;
        }

        public async Task<IEnumerable<PurchaseOrderItemDto>> GetPurchaseOrderItemsAsync(int poid)
        {
            var results = new List<PurchaseOrderItemDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT poi.*, p.SKU, p.ProductName,
                           ISNULL(p.ReorderLevel, 0) AS ReorderLevel,
                           ISNULL(stock.CurrentStock, 0) AS CurrentStock,
                           ISNULL(stock.AvailableStock, 0) AS AvailableStock
                    FROM PurchaseOrderItems poi
                    INNER JOIN Products p ON poi.ProductID = p.ProductID
                    OUTER APPLY (
                        SELECT 
                            SUM(CurrentQty) AS CurrentStock,
                            SUM(CurrentQty - ReservedQty - QCHoldQty - DamagedQty) AS AvailableStock
                        FROM InventoryStocks
                        WHERE ProductID = poi.ProductID
                    ) stock
                    WHERE poi.POID = @POID", connection);

                command.Parameters.AddWithValue("@POID", poid);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new PurchaseOrderItemDto
                    {
                        POItemID = reader.GetInt32("POItemID"),
                        POID = reader.GetInt32("POID"),
                        ProductID = reader.GetInt32("ProductID"),
                        SKU = reader.GetString("SKU"),
                        ProductName = reader.GetString("ProductName"),
                        OrderedQty = reader.GetDecimal("OrderedQty"),
                        ReceivedQty = reader.GetDecimal("ReceivedQty"),
                        ShippedQty = reader.IsDBNull("ShippedQty") ? (decimal?)null : reader.GetDecimal("ShippedQty"),
                        UnitPrice = reader.GetDecimal("UnitPrice"),
                        TotalPrice = reader.GetDecimal("TotalPrice"),
                        ReorderLevel = reader.GetDecimal("ReorderLevel"),
                        CurrentStock = reader.GetDecimal("CurrentStock"),
                        AvailableStock = reader.GetDecimal("AvailableStock")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching purchase order items.", ex);
            }

            return results;
        }

        public async Task<int> CreatePurchaseOrderWithItemsAsync(PurchaseOrderCreateDto po)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Calculate TotalAmount
                decimal totalAmount = 0;
                foreach (var item in po.Items)
                {
                    totalAmount += (item.OrderedQty * item.UnitPrice);
                }

                if (string.IsNullOrWhiteSpace(po.PONumber))
                {
                    po.PONumber = $"PO-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";
                }

                // Insert Header
                using var cmdHeader = new SqlCommand(@"
                    INSERT INTO PurchaseOrders (PONumber, SupplierID, OrderDate, ExpectedDate, TotalAmount, Status, Remarks, CreatedBy)
                    VALUES (@PONumber, @SupplierID, @OrderDate, @ExpectedDate, @TotalAmount, 'Draft', @Remarks, @CreatedBy);
                    SELECT SCOPE_IDENTITY();", connection, transaction);

                cmdHeader.Parameters.AddWithValue("@PONumber", po.PONumber);
                cmdHeader.Parameters.AddWithValue("@SupplierID", po.SupplierID);
                cmdHeader.Parameters.AddWithValue("@OrderDate", po.OrderDate);
                cmdHeader.Parameters.AddWithValue("@ExpectedDate", po.ExpectedDate ?? (object)DBNull.Value);
                cmdHeader.Parameters.AddWithValue("@TotalAmount", totalAmount);
                cmdHeader.Parameters.AddWithValue("@Remarks", po.Remarks ?? (object)DBNull.Value);
                cmdHeader.Parameters.AddWithValue("@CreatedBy", po.CreatedBy);

                var poId = Convert.ToInt32(await cmdHeader.ExecuteScalarAsync());

                // Insert Items
                foreach (var item in po.Items)
                {
                    using var cmdItem = new SqlCommand(@"
                        INSERT INTO PurchaseOrderItems (POID, ProductID, OrderedQty, UnitPrice)
                        VALUES (@POID, @ProductID, @OrderedQty, @UnitPrice)", 
                        connection, transaction);

                    cmdItem.Parameters.AddWithValue("@POID", poId);
                    cmdItem.Parameters.AddWithValue("@ProductID", item.ProductID);
                    cmdItem.Parameters.AddWithValue("@OrderedQty", item.OrderedQty);
                    cmdItem.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);

                    await cmdItem.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return poId;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception("Error creating Purchase Order.", ex);
            }
        }

        public async Task<bool> UpdatePurchaseOrderWithItemsAsync(PurchaseOrderUpdateDto po)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Calculate TotalAmount
                decimal totalAmount = 0;
                foreach (var item in po.Items)
                {
                    totalAmount += (item.OrderedQty * item.UnitPrice);
                }

                // Update Header
                using var cmdHeader = new SqlCommand(@"
                    UPDATE PurchaseOrders 
                    SET SupplierID = @SupplierID, OrderDate = @OrderDate, 
                        ExpectedDate = @ExpectedDate, TotalAmount = @TotalAmount, Remarks = @Remarks
                    WHERE POID = @POID AND Status = 'Draft'", connection, transaction);


                cmdHeader.Parameters.AddWithValue("@POID", po.POID);
                cmdHeader.Parameters.AddWithValue("@PONumber", po.PONumber);
                cmdHeader.Parameters.AddWithValue("@SupplierID", po.SupplierID);
                cmdHeader.Parameters.AddWithValue("@OrderDate", po.OrderDate);
                cmdHeader.Parameters.AddWithValue("@ExpectedDate", po.ExpectedDate ?? (object)DBNull.Value);
                cmdHeader.Parameters.AddWithValue("@TotalAmount", totalAmount);
                cmdHeader.Parameters.AddWithValue("@Remarks", po.Remarks ?? (object)DBNull.Value);

                var rowsAffected = await cmdHeader.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    // PO not found or not Draft
                    await transaction.RollbackAsync();
                    return false;
                }

                // Delete existing items
                using var cmdDelete = new SqlCommand("DELETE FROM PurchaseOrderItems WHERE POID = @POID", connection, transaction);
                cmdDelete.Parameters.AddWithValue("@POID", po.POID);
                await cmdDelete.ExecuteNonQueryAsync();

                // Insert new items
                foreach (var item in po.Items)
                {
                    using var cmdItem = new SqlCommand(@"
                        INSERT INTO PurchaseOrderItems (POID, ProductID, OrderedQty, UnitPrice)
                        VALUES (@POID, @ProductID, @OrderedQty, @UnitPrice)", 
                        connection, transaction);

                    cmdItem.Parameters.AddWithValue("@POID", po.POID);
                    cmdItem.Parameters.AddWithValue("@ProductID", item.ProductID);
                    cmdItem.Parameters.AddWithValue("@OrderedQty", item.OrderedQty);
                    cmdItem.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);

                    await cmdItem.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception("Error updating Purchase Order.", ex);
            }
        }

        public async Task<bool> ConfirmPurchaseOrderAsync(int poId, int approvedBy)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    UPDATE PurchaseOrders 
                    SET Status = 'Confirmed', ApprovedBy = @ApprovedBy
                    WHERE POID = @POID AND Status IN ('Draft', 'Pending Approval')", connection);

                command.Parameters.AddWithValue("@POID", poId);
                command.Parameters.AddWithValue("@ApprovedBy", approvedBy);

                await connection.OpenAsync();
                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error confirming Purchase Order.", ex);
            }
        }

        public async Task<bool> SubmitForApprovalAsync(int poId, int submittedBy)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    UPDATE PurchaseOrders 
                    SET Status = 'Pending Approval'
                    WHERE POID = @POID AND Status IN ('Draft', 'Rejected')", connection);

                command.Parameters.AddWithValue("@POID", poId);

                await connection.OpenAsync();
                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error submitting Purchase Order for approval.", ex);
            }
        }

        public async Task<bool> RejectApprovalAsync(int poId, int rejectedBy, string rejectionReason)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    UPDATE PurchaseOrders 
                    SET Status = 'Rejected', RejectionReason = @RejectionReason, ApprovedBy = @RejectedBy
                    WHERE POID = @POID AND Status IN ('Draft', 'Pending Approval')", connection);

                command.Parameters.AddWithValue("@POID", poId);
                command.Parameters.AddWithValue("@RejectedBy", rejectedBy);
                command.Parameters.AddWithValue("@RejectionReason", (object)rejectionReason ?? DBNull.Value);

                await connection.OpenAsync();
                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error rejecting Purchase Order approval.", ex);
            }
        }

        public async Task<bool> CancelPurchaseOrderAsync(int poId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    UPDATE PurchaseOrders 
                    SET Status = 'Cancelled'
                    WHERE POID = @POID AND Status IN ('Draft', 'Pending Approval', 'Confirmed')", connection);

                command.Parameters.AddWithValue("@POID", poId);

                await connection.OpenAsync();
                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error cancelling Purchase Order.", ex);
            }
        }

        public async Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(int poId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand("SELECT * FROM PurchaseOrders WHERE POID = @POID", connection);
                command.Parameters.AddWithValue("@POID", poId);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new PurchaseOrderDto
                    {
                        POID = reader.GetInt32("POID"),
                        PONumber = reader.GetString("PONumber"),
                        SupplierID = reader.GetInt32("SupplierID"),
                        OrderDate = reader.GetDateTime("OrderDate"),
                        ExpectedDate = reader.IsDBNull("ExpectedDate") ? (DateTime?)null : reader.GetDateTime("ExpectedDate"),
                        TotalAmount = reader.GetDecimal("TotalAmount"),
                        Status = reader.GetString("Status"),
                        CreatedBy = reader.GetInt32("CreatedBy"),
                        ApprovedBy = reader.IsDBNull("ApprovedBy") ? (int?)null : reader.GetInt32("ApprovedBy"),
                        Remarks = reader.IsDBNull("Remarks") ? "" : reader.GetString("Remarks"),
                        Carrier = reader.IsDBNull("Carrier") ? "" : reader.GetString("Carrier"),
                        TrackingNumber = reader.IsDBNull("TrackingNumber") ? "" : reader.GetString("TrackingNumber"),
                        RejectionReason = reader.IsDBNull("RejectionReason") ? "" : reader.GetString("RejectionReason")
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching purchase order details.", ex);
            }
        }

        public async Task<bool> IsPONumberExistsAsync(string poNumber)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT COUNT(*) FROM PurchaseOrders WHERE PONumber = @PONumber", connection);
            command.Parameters.AddWithValue("@PONumber", poNumber);
            await connection.OpenAsync();
            var count = (int)(await command.ExecuteScalarAsync() ?? 0);
            return count > 0;
        }

        public async Task<bool> IsPONumberExistsForOtherPOAsync(string poNumber, int poId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT COUNT(*) FROM PurchaseOrders WHERE PONumber = @PONumber AND POID != @POID", connection);
            command.Parameters.AddWithValue("@PONumber", poNumber);
            command.Parameters.AddWithValue("@POID", poId);
            await connection.OpenAsync();
            var count = (int)(await command.ExecuteScalarAsync() ?? 0);
            return count > 0;
        }

        public async Task<IEnumerable<dynamic>> GetActiveSuppliersAsync()
        {
            var results = new List<dynamic>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT SupplierID, SupplierCode, SupplierName FROM Suppliers WHERE Status = 'Active' ORDER BY SupplierName", connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new { ID = reader.GetInt32(0), Code = reader.GetString(1), Name = reader.GetString(2) });
            }
            return results;
        }

        public async Task<IEnumerable<dynamic>> GetActiveProductsAsync()
        {
            var results = new List<dynamic>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT p.ProductID, p.SKU, p.ProductName, ISNULL(p.ProductType, '') AS ProductType, ISNULL(c.CategoryName, '') AS CategoryName
                FROM Products p
                LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                WHERE p.Status = 'Active'
                ORDER BY ISNULL(p.ProductType, ''), ISNULL(c.CategoryName, ''), p.ProductName", connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new
                {
                    ID = reader.GetInt32(0),
                    SKU = reader.GetString(1),
                    Name = reader.GetString(2),
                    ProductType = reader.GetString(3),
                    Category = reader.GetString(4)
                });
            }
            return results;
        }

        public async Task<IEnumerable<SupplierProductDto>> GetSupplierProductMappingsAsync()
        {
            var results = new List<SupplierProductDto>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT SupplierProductID, SupplierID, ProductID, SupplierSKU, LastQuotedPrice, LeadTimeDays, MinOrderQty, PackSize, IsPreferred, Status
                FROM SupplierProducts
                WHERE Status = 'Approved'
                ORDER BY SupplierID, IsPreferred DESC, ProductID", connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new SupplierProductDto
                {
                    SupplierProductID = reader.GetInt32("SupplierProductID"),
                    SupplierID = reader.GetInt32("SupplierID"),
                    ProductID = reader.GetInt32("ProductID"),
                    SupplierSKU = reader.IsDBNull("SupplierSKU") ? "" : reader.GetString("SupplierSKU"),
                    LastQuotedPrice = reader.IsDBNull("LastQuotedPrice") ? null : reader.GetDecimal("LastQuotedPrice"),
                    LeadTimeDays = reader.GetInt32("LeadTimeDays"),
                    MinOrderQty = reader.GetDecimal("MinOrderQty"),
                    PackSize = reader.GetDecimal("PackSize"),
                    IsPreferred = reader.GetBoolean("IsPreferred"),
                    Status = reader.GetString("Status")
                });
            }
            return results;
        }

        public async Task<bool> IsSupplierProductApprovedAsync(int supplierId, int productId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT COUNT(*)
                FROM SupplierProducts
                WHERE SupplierID = @SupplierID
                    AND ProductID = @ProductID
                    AND Status = 'Approved'", connection);
            command.Parameters.AddWithValue("@SupplierID", supplierId);
            command.Parameters.AddWithValue("@ProductID", productId);

            await connection.OpenAsync();
            return Convert.ToInt32(await command.ExecuteScalarAsync() ?? 0) > 0;
        }

        public async Task<int> UpsertSupplierProductAsync(SupplierProductDto mapping)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                using var command = new SqlCommand(@"
                    DECLARE @SupplierProductID INT;
                    IF EXISTS (SELECT 1 FROM SupplierProducts WHERE SupplierID = @SupplierID AND ProductID = @ProductID)
                    BEGIN
                        UPDATE SupplierProducts
                        SET SupplierSKU = @SupplierSKU,
                            LastQuotedPrice = @LastQuotedPrice,
                            LeadTimeDays = @LeadTimeDays,
                            MinOrderQty = @MinOrderQty,
                            PackSize = @PackSize,
                            IsPreferred = @IsPreferred,
                            Status = @Status,
                            UpdatedAt = GETDATE()
                        WHERE SupplierID = @SupplierID AND ProductID = @ProductID;

                        SELECT @SupplierProductID = SupplierProductID FROM SupplierProducts WHERE SupplierID = @SupplierID AND ProductID = @ProductID;
                    END
                    ELSE
                    BEGIN
                        INSERT INTO SupplierProducts (SupplierID, ProductID, SupplierSKU, LastQuotedPrice, LeadTimeDays, MinOrderQty, PackSize, IsPreferred, Status)
                        VALUES (@SupplierID, @ProductID, @SupplierSKU, @LastQuotedPrice, @LeadTimeDays, @MinOrderQty, @PackSize, @IsPreferred, @Status);

                        SELECT @SupplierProductID = SCOPE_IDENTITY();
                    END

                    -- Preferred supplier constraint enforcement
                    IF @IsPreferred = 1
                    BEGIN
                        UPDATE SupplierProducts
                        SET IsPreferred = 0
                        WHERE ProductID = @ProductID AND SupplierID <> @SupplierID;

                        -- Auto-sync quote price to Product Standard Cost
                        IF @LastQuotedPrice IS NOT NULL AND @LastQuotedPrice > 0
                        BEGIN
                            UPDATE Products
                            SET StandardCost = @LastQuotedPrice, UpdatedAt = GETDATE()
                            WHERE ProductID = @ProductID;
                        END
                    END

                    SELECT @SupplierProductID;", connection, transaction);

                command.Parameters.AddWithValue("@SupplierID", mapping.SupplierID);
                command.Parameters.AddWithValue("@ProductID", mapping.ProductID);
                command.Parameters.AddWithValue("@SupplierSKU", string.IsNullOrWhiteSpace(mapping.SupplierSKU) ? DBNull.Value : mapping.SupplierSKU);
                command.Parameters.AddWithValue("@LastQuotedPrice", mapping.LastQuotedPrice.HasValue ? mapping.LastQuotedPrice.Value : DBNull.Value);
                command.Parameters.AddWithValue("@LeadTimeDays", mapping.LeadTimeDays);
                command.Parameters.AddWithValue("@MinOrderQty", mapping.MinOrderQty <= 0 ? 1 : mapping.MinOrderQty);
                command.Parameters.AddWithValue("@PackSize", mapping.PackSize <= 0 ? 1 : mapping.PackSize);
                command.Parameters.AddWithValue("@IsPreferred", mapping.IsPreferred);
                command.Parameters.AddWithValue("@Status", string.IsNullOrWhiteSpace(mapping.Status) ? "Approved" : mapping.Status);

                var id = Convert.ToInt32(await command.ExecuteScalarAsync());
                await transaction.CommitAsync();
                return id;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception("Error saving supplier-product mapping.", ex);
            }
        }

        public async Task<bool> SetSupplierProductStatusAsync(int supplierProductId, string status)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                UPDATE SupplierProducts
                SET Status = @Status, 
                    IsPreferred = CASE WHEN @Status IN ('Blocked', 'Inactive') THEN 0 ELSE IsPreferred END,
                    UpdatedAt = GETDATE()
                WHERE SupplierProductID = @SupplierProductID", connection);

            command.Parameters.AddWithValue("@SupplierProductID", supplierProductId);
            command.Parameters.AddWithValue("@Status", status);

            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<Dictionary<int, ComponentProcurementDto>> GetProcurementDataForProductsAsync(List<int> productIds)
        {
            var result = new Dictionary<int, ComponentProcurementDto>();
            if (productIds == null || !productIds.Any()) return result;

            try
            {
                var idList = string.Join(",", productIds);
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Query 1: Get StandardCost for all products first (as baseline)
                using (var cmdBase = new SqlCommand($@"
                    SELECT ProductID, StandardCost 
                    FROM Products 
                    WHERE ProductID IN ({idList})", connection))
                {
                    using var reader = await cmdBase.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var pid = reader.GetInt32(reader.GetOrdinal("ProductID"));
                        result[pid] = new ComponentProcurementDto
                        {
                            ProductID = pid,
                            StandardCost = reader.IsDBNull(reader.GetOrdinal("StandardCost")) ? 0 : reader.GetDecimal(reader.GetOrdinal("StandardCost"))
                        };
                    }
                }

                // Query 2: Get latest PO price for each product (most recent non-cancelled/non-draft PO)
                using (var cmdLatest = new SqlCommand($@"
                    ;WITH LatestPO AS (
                        SELECT poi.ProductID, poi.UnitPrice, po.PONumber, po.OrderDate, s.SupplierName,
                               ROW_NUMBER() OVER (PARTITION BY poi.ProductID ORDER BY po.OrderDate DESC) AS rn
                        FROM PurchaseOrderItems poi
                        INNER JOIN PurchaseOrders po ON poi.POID = po.POID
                        INNER JOIN Suppliers s ON po.SupplierID = s.SupplierID
                        WHERE poi.ProductID IN ({idList})
                          AND po.Status NOT IN ('Cancelled', 'Draft')
                    )
                    SELECT ProductID, UnitPrice AS LastPOUnitPrice, PONumber AS LastPONumber, 
                           OrderDate AS LastPODate, SupplierName
                    FROM LatestPO WHERE rn = 1", connection))
                {
                    using var reader = await cmdLatest.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var pid = reader.GetInt32(reader.GetOrdinal("ProductID"));
                        if (!result.ContainsKey(pid))
                            result[pid] = new ComponentProcurementDto { ProductID = pid };

                        result[pid].LastPOUnitPrice = reader.GetDecimal(reader.GetOrdinal("LastPOUnitPrice"));
                        result[pid].LastPONumber = reader.GetString(reader.GetOrdinal("LastPONumber"));
                        result[pid].LastPODate = reader.GetDateTime(reader.GetOrdinal("LastPODate"));
                        result[pid].SupplierName = reader.GetString(reader.GetOrdinal("SupplierName"));
                    }
                }

                // Query 3: Get earliest open PO delivery date for each product
                using (var cmdDelivery = new SqlCommand($@"
                    ;WITH EarliestDelivery AS (
                        SELECT poi.ProductID, po.ExpectedDate, po.PONumber,
                               ROW_NUMBER() OVER (PARTITION BY poi.ProductID ORDER BY po.ExpectedDate ASC) AS rn
                        FROM PurchaseOrderItems poi
                        INNER JOIN PurchaseOrders po ON poi.POID = po.POID
                        WHERE poi.ProductID IN ({idList})
                          AND po.Status IN ('Confirmed', 'InTransit')
                          AND po.ExpectedDate IS NOT NULL
                    )
                    SELECT ProductID, ExpectedDate AS OpenPOExpectedDate, PONumber AS OpenPONumber
                    FROM EarliestDelivery WHERE rn = 1", connection))
                {
                    using var reader = await cmdDelivery.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var pid = reader.GetInt32(reader.GetOrdinal("ProductID"));
                        if (!result.ContainsKey(pid))
                            result[pid] = new ComponentProcurementDto { ProductID = pid };

                        result[pid].OpenPOExpectedDate = reader.GetDateTime(reader.GetOrdinal("OpenPOExpectedDate"));
                        result[pid].OpenPONumber = reader.GetString(reader.GetOrdinal("OpenPONumber"));
                    }
                }
            }
            catch (Exception)
            {
                // Return whatever we have, or empty on complete failure
            }

            return result;
        }

        public async Task<bool> ShipPurchaseOrderAsync(int poId, int supplierId, string carrier, string trackingNumber, DateTime expectedDate, Dictionary<int, decimal> itemShippedQuantities)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Update PO Header
                using var cmdHeader = new SqlCommand(@"
                    UPDATE PurchaseOrders
                    SET Status = 'InTransit', Carrier = @Carrier, TrackingNumber = @TrackingNumber, ExpectedDate = @ExpectedDate
                    WHERE POID = @POID AND SupplierID = @SupplierID AND Status = 'Confirmed'", connection, transaction);

                cmdHeader.Parameters.AddWithValue("@POID", poId);
                cmdHeader.Parameters.AddWithValue("@SupplierID", supplierId);
                cmdHeader.Parameters.AddWithValue("@Carrier", (object)carrier ?? DBNull.Value);
                cmdHeader.Parameters.AddWithValue("@TrackingNumber", (object)trackingNumber ?? DBNull.Value);
                cmdHeader.Parameters.AddWithValue("@ExpectedDate", expectedDate);

                int affected = await cmdHeader.ExecuteNonQueryAsync();
                if (affected == 0)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                // Update PO Items
                foreach (var kvp in itemShippedQuantities)
                {
                    using var cmdItem = new SqlCommand(@"
                        UPDATE PurchaseOrderItems
                        SET ShippedQty = @ShippedQty
                        WHERE POItemID = @POItemID AND POID = @POID", connection, transaction);

                    cmdItem.Parameters.AddWithValue("@POItemID", kvp.Key);
                    cmdItem.Parameters.AddWithValue("@POID", poId);
                    cmdItem.Parameters.AddWithValue("@ShippedQty", kvp.Value);

                    await cmdItem.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception("Error shipping Purchase Order in database.", ex);
            }
        }

        public async Task<bool> RejectPurchaseOrderAsync(int poId, string rejectionReason)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    UPDATE PurchaseOrders 
                    SET Status = 'Cancelled', RejectionReason = @RejectionReason
                    WHERE POID = @POID AND Status = 'Confirmed'", connection);

                command.Parameters.AddWithValue("@POID", poId);
                command.Parameters.AddWithValue("@RejectionReason", rejectionReason);

                await connection.OpenAsync();
                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error rejecting Purchase Order in repository.", ex);
            }
        }

        public async Task<PurchaseOrderDto?> GetPurchaseOrderDetailsAsync(int poId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT po.*, 
                           s.SupplierName,
                           ISNULL(s.PaymentTerms, 'Net 30') AS SupplierPaymentTerms,
                           ISNULL(s.LeadTimeDays, 0) AS SupplierLeadTimeDays,
                           ISNULL(s.Status, 'Active') AS SupplierStatus,
                           uc.FullName AS CreatedByName, 
                           ua.FullName AS ApprovedByName,
                           items.ItemCount,
                           items.TotalOrderedQty,
                           items.TotalReceivedQty
                    FROM PurchaseOrders po
                    INNER JOIN Suppliers s ON po.SupplierID = s.SupplierID
                    LEFT JOIN Users uc ON po.CreatedBy = uc.UserID
                    LEFT JOIN Users ua ON po.ApprovedBy = ua.UserID
                    LEFT JOIN (
                        SELECT POID, 
                               COUNT(*) AS ItemCount, 
                               SUM(OrderedQty) AS TotalOrderedQty, 
                               SUM(ReceivedQty) AS TotalReceivedQty
                        FROM PurchaseOrderItems
                        GROUP BY POID
                    ) items ON po.POID = items.POID
                    WHERE po.POID = @POID", connection);

                command.Parameters.AddWithValue("@POID", poId);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new PurchaseOrderDto
                    {
                        POID = reader.GetInt32("POID"),
                        PONumber = reader.GetString("PONumber"),
                        SupplierID = reader.GetInt32("SupplierID"),
                        SupplierName = reader.GetString("SupplierName"),
                        SupplierPaymentTerms = reader.GetString("SupplierPaymentTerms"),
                        SupplierLeadTimeDays = reader.GetInt32("SupplierLeadTimeDays"),
                        SupplierStatus = reader.GetString("SupplierStatus"),
                        OrderDate = reader.GetDateTime("OrderDate"),
                        ExpectedDate = reader.IsDBNull("ExpectedDate") ? (DateTime?)null : reader.GetDateTime("ExpectedDate"),
                        TotalAmount = reader.GetDecimal("TotalAmount"),
                        Status = reader.GetString("Status"),
                        CreatedBy = reader.GetInt32("CreatedBy"),
                        CreatedByName = reader.IsDBNull("CreatedByName") ? "" : reader.GetString("CreatedByName"),
                        ApprovedBy = reader.IsDBNull("ApprovedBy") ? (int?)null : reader.GetInt32("ApprovedBy"),
                        ApprovedByName = reader.IsDBNull("ApprovedByName") ? "" : reader.GetString("ApprovedByName"),
                        Remarks = reader.IsDBNull("Remarks") ? "" : reader.GetString("Remarks"),
                        Carrier = reader.IsDBNull("Carrier") ? "" : reader.GetString("Carrier"),
                        TrackingNumber = reader.IsDBNull("TrackingNumber") ? "" : reader.GetString("TrackingNumber"),
                        RejectionReason = reader.IsDBNull("RejectionReason") ? "" : reader.GetString("RejectionReason"),
                        ItemCount = reader.IsDBNull("ItemCount") ? 0 : reader.GetInt32("ItemCount"),
                        TotalOrderedQty = reader.IsDBNull("TotalOrderedQty") ? 0 : reader.GetDecimal("TotalOrderedQty"),
                        TotalReceivedQty = reader.IsDBNull("TotalReceivedQty") ? 0 : reader.GetDecimal("TotalReceivedQty")
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching purchase order details.", ex);
            }
        }

        public async Task<(IEnumerable<PurchaseOrderDto> Orders, int TotalCount)> GetPagedPurchaseOrdersAsync(
            int offset, int limit, string? search, string? supplier, string? status, int? supplierIdScope = null)
        {
            var orders = new List<PurchaseOrderDto>();
            int totalCount = 0;

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // 1. Get the total count of matching orders
                var countSql = @"
                    SELECT COUNT(*)
                    FROM PurchaseOrders po
                    INNER JOIN Suppliers s ON po.SupplierID = s.SupplierID
                    WHERE (@SupplierIdScope IS NULL OR po.SupplierID = @SupplierIdScope)
                      AND (@Status = 'All Statuses' OR po.Status = @Status)
                      AND (@Supplier = 'All Suppliers' OR s.SupplierName = @Supplier)
                      AND (@Search IS NULL OR @Search = '' 
                           OR po.PONumber LIKE '%' + @Search + '%' 
                           OR s.SupplierName LIKE '%' + @Search + '%' 
                           OR po.Remarks LIKE '%' + @Search + '%')";

                using var countCommand = new SqlCommand(countSql, connection);
                countCommand.Parameters.AddWithValue("@SupplierIdScope", (object?)supplierIdScope ?? DBNull.Value);
                countCommand.Parameters.AddWithValue("@Status", string.IsNullOrWhiteSpace(status) ? "All Statuses" : status);
                countCommand.Parameters.AddWithValue("@Supplier", string.IsNullOrWhiteSpace(supplier) ? "All Suppliers" : supplier);
                countCommand.Parameters.AddWithValue("@Search", string.IsNullOrWhiteSpace(search) ? "" : search);

                totalCount = (int)(await countCommand.ExecuteScalarAsync() ?? 0);

                // 2. Get the paginated orders
                var dataSql = @"
                    SELECT po.*, 
                           s.SupplierName, 
                           uc.FullName AS CreatedByName, 
                           ua.FullName AS ApprovedByName,
                           items.ItemCount,
                           items.TotalOrderedQty,
                           items.TotalReceivedQty
                    FROM PurchaseOrders po
                    INNER JOIN Suppliers s ON po.SupplierID = s.SupplierID
                    LEFT JOIN Users uc ON po.CreatedBy = uc.UserID
                    LEFT JOIN Users ua ON po.ApprovedBy = ua.UserID
                    LEFT JOIN (
                        SELECT POID, 
                               COUNT(*) AS ItemCount, 
                               SUM(OrderedQty) AS TotalOrderedQty, 
                               SUM(ReceivedQty) AS TotalReceivedQty
                        FROM PurchaseOrderItems
                        GROUP BY POID
                    ) items ON po.POID = items.POID
                    WHERE (@SupplierIdScope IS NULL OR po.SupplierID = @SupplierIdScope)
                      AND (@Status = 'All Statuses' OR po.Status = @Status)
                      AND (@Supplier = 'All Suppliers' OR s.SupplierName = @Supplier)
                      AND (@Search IS NULL OR @Search = '' 
                           OR po.PONumber LIKE '%' + @Search + '%' 
                           OR s.SupplierName LIKE '%' + @Search + '%' 
                           OR po.Remarks LIKE '%' + @Search + '%')
                    ORDER BY po.OrderDate DESC
                    OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY";

                using var dataCommand = new SqlCommand(dataSql, connection);
                dataCommand.Parameters.AddWithValue("@SupplierIdScope", (object?)supplierIdScope ?? DBNull.Value);
                dataCommand.Parameters.AddWithValue("@Status", string.IsNullOrWhiteSpace(status) ? "All Statuses" : status);
                dataCommand.Parameters.AddWithValue("@Supplier", string.IsNullOrWhiteSpace(supplier) ? "All Suppliers" : supplier);
                dataCommand.Parameters.AddWithValue("@Search", string.IsNullOrWhiteSpace(search) ? "" : search);
                dataCommand.Parameters.AddWithValue("@Offset", offset);
                dataCommand.Parameters.AddWithValue("@Limit", limit);

                using var reader = await dataCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    orders.Add(new PurchaseOrderDto
                    {
                        POID = reader.GetInt32("POID"),
                        PONumber = reader.GetString("PONumber"),
                        SupplierID = reader.GetInt32("SupplierID"),
                        SupplierName = reader.GetString("SupplierName"),
                        OrderDate = reader.GetDateTime("OrderDate"),
                        ExpectedDate = reader.IsDBNull("ExpectedDate") ? (DateTime?)null : reader.GetDateTime("ExpectedDate"),
                        TotalAmount = reader.GetDecimal("TotalAmount"),
                        Status = reader.GetString("Status"),
                        CreatedBy = reader.GetInt32("CreatedBy"),
                        CreatedByName = reader.IsDBNull("CreatedByName") ? "" : reader.GetString("CreatedByName"),
                        ApprovedBy = reader.IsDBNull("ApprovedBy") ? (int?)null : reader.GetInt32("ApprovedBy"),
                        ApprovedByName = reader.IsDBNull("ApprovedByName") ? "" : reader.GetString("ApprovedByName"),
                        Remarks = reader.IsDBNull("Remarks") ? "" : reader.GetString("Remarks"),
                        Carrier = reader.IsDBNull("Carrier") ? "" : reader.GetString("Carrier"),
                        TrackingNumber = reader.IsDBNull("TrackingNumber") ? "" : reader.GetString("TrackingNumber"),
                        RejectionReason = reader.IsDBNull("RejectionReason") ? "" : reader.GetString("RejectionReason"),
                        ItemCount = reader.IsDBNull("ItemCount") ? 0 : reader.GetInt32("ItemCount"),
                        TotalOrderedQty = reader.IsDBNull("TotalOrderedQty") ? 0 : reader.GetDecimal("TotalOrderedQty"),
                        TotalReceivedQty = reader.IsDBNull("TotalReceivedQty") ? 0 : reader.GetDecimal("TotalReceivedQty")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching paged purchase orders.", ex);
            }

            return (orders, totalCount);
        }
    }
}

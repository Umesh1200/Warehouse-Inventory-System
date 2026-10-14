using IMS.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public class StockTransferRepository : IStockTransferRepository
    {
        private readonly string _connectionString;
        private static bool _assignedToColumnEnsured = false;

        public StockTransferRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");
        }

        private async Task EnsureAssignedToColumnExistsAsync(SqlConnection connection)
        {
            if (_assignedToColumnEnsured) return;
            try
            {
                using var cmd = new SqlCommand(@"
                    IF COL_LENGTH('Transfers', 'AssignedTo') IS NULL
                    BEGIN
                        ALTER TABLE Transfers ADD AssignedTo INT NULL CONSTRAINT FK_Transfers_AssignedTo REFERENCES Users(UserID);
                    END", connection);
                await cmd.ExecuteNonQueryAsync();
                _assignedToColumnEnsured = true;
            }
            catch
            {
                // In case of concurrent creation or permission, mark true
                _assignedToColumnEnsured = true;
            }
        }

        public async Task<IEnumerable<StockTransferDto>> GetAllTransfersAsync()
        {
            var results = new List<StockTransferDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                await EnsureAssignedToColumnExistsAsync(connection);

                using var command = new SqlCommand(@"
                    SELECT t.TransferID, t.TransferNo, t.TransferType, t.RequestedBy, t.ApprovedBy, t.AssignedTo, t.Status, t.RequestDate, t.CompletionDate,
                           ur.FullName AS RequestedByName, 
                           ua.FullName AS ApprovedByName,
                           uas.FullName AS AssignedToName,
                           items.ItemCount,
                           items.TotalQuantity,
                           p.ProductName AS FirstProductName,
                           p.SKU AS FirstSKU,
                           w_from.WarehouseName AS FromWarehouseName,
                           sh_from.ShelfCode AS FromShelfCode,
                           w_to.WarehouseName AS ToWarehouseName,
                           sh_to.ShelfCode AS ToShelfCode
                    FROM Transfers t
                    LEFT JOIN Users ur ON t.RequestedBy = ur.UserID
                    LEFT JOIN Users ua ON t.ApprovedBy = ua.UserID
                    LEFT JOIN Users uas ON t.AssignedTo = uas.UserID
                    LEFT JOIN (
                        SELECT TransferID, 
                               COUNT(*) AS ItemCount, 
                               SUM(Quantity) AS TotalQuantity,
                               MIN(TransferItemID) AS MinItemID
                        FROM TransferItems
                        GROUP BY TransferID
                    ) items ON t.TransferID = items.TransferID
                    LEFT JOIN TransferItems ti_first ON items.MinItemID = ti_first.TransferItemID
                    LEFT JOIN Products p ON ti_first.ProductID = p.ProductID
                    LEFT JOIN Shelves sh_from ON ti_first.FromShelfID = sh_from.ShelfID
                    LEFT JOIN Racks rk_from ON sh_from.RackID = rk_from.RackID
                    LEFT JOIN Rooms rm_from ON rk_from.RoomID = rm_from.RoomID
                    LEFT JOIN Floors f_from ON rm_from.FloorID = f_from.FloorID
                    LEFT JOIN Warehouses w_from ON f_from.WarehouseID = w_from.WarehouseID
                    LEFT JOIN Shelves sh_to ON ti_first.ToShelfID = sh_to.ShelfID
                    LEFT JOIN Racks rk_to ON sh_to.RackID = rk_to.RackID
                    LEFT JOIN Rooms rm_to ON rk_to.RoomID = rm_to.RoomID
                    LEFT JOIN Floors f_to ON rm_to.FloorID = f_to.FloorID
                    LEFT JOIN Warehouses w_to ON f_to.WarehouseID = w_to.WarehouseID
                    ORDER BY t.RequestDate DESC", connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new StockTransferDto
                    {
                        TransferID = reader.GetInt32("TransferID"),
                        TransferNo = reader.GetString("TransferNo"),
                        TransferType = reader.IsDBNull("TransferType") ? "" : reader.GetString("TransferType"),
                        RequestedBy = reader.GetInt32("RequestedBy"),
                        RequestedByName = reader.IsDBNull("RequestedByName") ? "" : reader.GetString("RequestedByName"),
                        ApprovedBy = reader.IsDBNull("ApprovedBy") ? (int?)null : reader.GetInt32("ApprovedBy"),
                        ApprovedByName = reader.IsDBNull("ApprovedByName") ? "" : reader.GetString("ApprovedByName"),
                        AssignedTo = reader.IsDBNull("AssignedTo") ? (int?)null : reader.GetInt32("AssignedTo"),
                        AssignedToName = reader.IsDBNull("AssignedToName") ? "" : reader.GetString("AssignedToName"),
                        Status = reader.GetString("Status"),
                        RequestDate = reader.GetDateTime("RequestDate"),
                        CompletionDate = reader.IsDBNull("CompletionDate") ? (DateTime?)null : reader.GetDateTime("CompletionDate"),
                        ItemCount = reader.IsDBNull("ItemCount") ? 0 : reader.GetInt32("ItemCount"),
                        TotalQuantity = reader.IsDBNull("TotalQuantity") ? 0 : reader.GetDecimal("TotalQuantity"),
                        FirstProductName = reader.IsDBNull("FirstProductName") ? "" : reader.GetString("FirstProductName"),
                        FirstSKU = reader.IsDBNull("FirstSKU") ? "" : reader.GetString("FirstSKU"),
                        FromWarehouseName = reader.IsDBNull("FromWarehouseName") ? "" : reader.GetString("FromWarehouseName"),
                        FromShelfCode = reader.IsDBNull("FromShelfCode") ? "" : reader.GetString("FromShelfCode"),
                        ToWarehouseName = reader.IsDBNull("ToWarehouseName") ? "" : reader.GetString("ToWarehouseName"),
                        ToShelfCode = reader.IsDBNull("ToShelfCode") ? "" : reader.GetString("ToShelfCode")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching stock transfers.", ex);
            }

            return results;
        }

        public async Task<IEnumerable<StockTransferItemDto>> GetTransferItemsAsync(int transferId)
        {
            var results = new List<StockTransferItemDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT ti.*, 
                           p.SKU, p.ProductName,
                           sh_from.ShelfCode AS FromShelfCode, 
                           w_from.WarehouseName AS FromWarehouseName, 
                           f_from.FloorName AS FromFloorName, 
                           rm_from.RoomName AS FromRoomName, 
                           rk_from.RackName AS FromRackName,
                           sh_to.ShelfCode AS ToShelfCode, 
                           w_to.WarehouseName AS ToWarehouseName, 
                           f_to.FloorName AS ToFloorName, 
                           rm_to.RoomName AS ToRoomName, 
                           rk_to.RackName AS ToRackName
                    FROM TransferItems ti
                    INNER JOIN Products p ON ti.ProductID = p.ProductID
                    -- From Hierarchy
                    INNER JOIN Shelves sh_from ON ti.FromShelfID = sh_from.ShelfID
                    INNER JOIN Racks rk_from ON sh_from.RackID = rk_from.RackID
                    INNER JOIN Rooms rm_from ON rk_from.RoomID = rm_from.RoomID
                    INNER JOIN Floors f_from ON rm_from.FloorID = f_from.FloorID
                    INNER JOIN Warehouses w_from ON f_from.WarehouseID = w_from.WarehouseID
                    -- To Hierarchy
                    INNER JOIN Shelves sh_to ON ti.ToShelfID = sh_to.ShelfID
                    INNER JOIN Racks rk_to ON sh_to.RackID = rk_to.RackID
                    INNER JOIN Rooms rm_to ON rk_to.RoomID = rm_to.RoomID
                    INNER JOIN Floors f_to ON rm_to.FloorID = f_to.FloorID
                    INNER JOIN Warehouses w_to ON f_to.WarehouseID = w_to.WarehouseID
                    WHERE ti.TransferID = @TransferID", connection);

                command.Parameters.AddWithValue("@TransferID", transferId);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new StockTransferItemDto
                    {
                        TransferItemID = reader.GetInt32("TransferItemID"),
                        TransferID = reader.GetInt32("TransferID"),
                        ProductID = reader.GetInt32("ProductID"),
                        SKU = reader.GetString("SKU"),
                        ProductName = reader.GetString("ProductName"),
                        FromShelfID = reader.GetInt32("FromShelfID"),
                        FromShelfCode = reader.GetString("FromShelfCode"),
                        FromWarehouseName = reader.GetString("FromWarehouseName"),
                        FromFloorName = reader.IsDBNull("FromFloorName") ? "" : reader.GetString("FromFloorName"),
                        FromRoomName = reader.IsDBNull("FromRoomName") ? "" : reader.GetString("FromRoomName"),
                        FromRackName = reader.IsDBNull("FromRackName") ? "" : reader.GetString("FromRackName"),
                        ToShelfID = reader.GetInt32("ToShelfID"),
                        ToShelfCode = reader.GetString("ToShelfCode"),
                        ToWarehouseName = reader.GetString("ToWarehouseName"),
                        ToFloorName = reader.IsDBNull("ToFloorName") ? "" : reader.GetString("ToFloorName"),
                        ToRoomName = reader.IsDBNull("ToRoomName") ? "" : reader.GetString("ToRoomName"),
                        ToRackName = reader.IsDBNull("ToRackName") ? "" : reader.GetString("ToRackName"),
                        Quantity = reader.GetDecimal("Quantity")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching stock transfer items.", ex);
            }

            return results;
        }

        public async Task<int> CreateTransferWithItemsAsync(StockTransferCreateDto transfer)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Ensure AssignedTo column exists
                using var cmdEnsure = new SqlCommand(@"
                    IF COL_LENGTH('Transfers', 'AssignedTo') IS NULL
                    BEGIN
                        ALTER TABLE Transfers ADD AssignedTo INT NULL CONSTRAINT FK_Transfers_AssignedTo REFERENCES Users(UserID);
                    END", connection, transaction);
                await cmdEnsure.ExecuteNonQueryAsync();

                // 1. Insert Header
                using var cmdHeader = new SqlCommand(@"
                    INSERT INTO Transfers (TransferNo, TransferType, RequestedBy, AssignedTo, Status, RequestDate)
                    VALUES (@TransferNo, @TransferType, @RequestedBy, @AssignedTo, 'Awaiting Approval', GETDATE());
                    SELECT SCOPE_IDENTITY();", connection, transaction);

                cmdHeader.Parameters.AddWithValue("@TransferNo", transfer.TransferNo);
                cmdHeader.Parameters.AddWithValue("@TransferType", transfer.TransferType);
                cmdHeader.Parameters.AddWithValue("@RequestedBy", transfer.RequestedBy);
                cmdHeader.Parameters.AddWithValue("@AssignedTo", (object?)transfer.AssignedTo ?? DBNull.Value);

                var transferId = Convert.ToInt32(await cmdHeader.ExecuteScalarAsync());

                // 2. Insert Items
                foreach (var item in transfer.Items)
                {
                    using var cmdItem = new SqlCommand(@"
                        INSERT INTO TransferItems (TransferID, ProductID, FromShelfID, ToShelfID, Quantity)
                        VALUES (@TransferID, @ProductID, @FromShelfID, @ToShelfID, @Quantity)", 
                        connection, transaction);

                    cmdItem.Parameters.AddWithValue("@TransferID", transferId);
                    cmdItem.Parameters.AddWithValue("@ProductID", item.ProductID);
                    cmdItem.Parameters.AddWithValue("@FromShelfID", item.FromShelfID);
                    cmdItem.Parameters.AddWithValue("@ToShelfID", item.ToShelfID);
                    cmdItem.Parameters.AddWithValue("@Quantity", item.Quantity);

                    await cmdItem.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return transferId;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception("Error creating transfer request.", ex);
            }
        }

        public async Task<StockTransferDto?> GetTransferByIdAsync(int transferId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand("SELECT * FROM Transfers WHERE TransferID = @TransferID", connection);
                command.Parameters.AddWithValue("@TransferID", transferId);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new StockTransferDto
                    {
                        TransferID = reader.GetInt32("TransferID"),
                        TransferNo = reader.GetString("TransferNo"),
                        TransferType = reader.IsDBNull("TransferType") ? "" : reader.GetString("TransferType"),
                        RequestedBy = reader.GetInt32("RequestedBy"),
                        Status = reader.GetString("Status"),
                        RequestDate = reader.GetDateTime("RequestDate")
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching transfer details.", ex);
            }
        }

        public async Task<bool> ApproveTransferAsync(int transferId, int approvedBy)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    UPDATE Transfers 
                    SET Status = 'Approved', ApprovedBy = @ApprovedBy
                    WHERE TransferID = @TransferID", connection);

                command.Parameters.AddWithValue("@TransferID", transferId);
                command.Parameters.AddWithValue("@ApprovedBy", approvedBy);

                await connection.OpenAsync();
                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error approving transfer.", ex);
            }
        }

        public async Task<bool> CancelTransferAsync(int transferId, int cancelledBy)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    UPDATE Transfers 
                    SET Status = 'Cancelled'
                    WHERE TransferID = @TransferID AND Status IN ('Pending', 'Awaiting Approval', 'Approved')", connection);

                command.Parameters.AddWithValue("@TransferID", transferId);

                await connection.OpenAsync();
                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error cancelling transfer.", ex);
            }
        }

        public async Task<StockTransferActionResultDto> PostTransferAsync(int transferId, int performedBy)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand("dbo.sp_TransferStock", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@TransferID", transferId);
                command.Parameters.AddWithValue("@PerformedBy", performedBy);

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();

                return new StockTransferActionResultDto { Success = true, Message = "Transfer completed successfully. Stock moved." };
            }
            catch (SqlException ex)
            {
                return new StockTransferActionResultDto { Success = false, Message = ex.Message };
            }
            catch (Exception ex)
            {
                return new StockTransferActionResultDto { Success = false, Message = "An unexpected error occurred during posting. " + ex.Message };
            }
        }

        public async Task<decimal> GetAvailableStockAsync(int productId, int shelfId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT dbo.fn_GetAvailableStock(@ProductID, @ShelfID)", connection);

            command.Parameters.AddWithValue("@ProductID", productId);
            command.Parameters.AddWithValue("@ShelfID", shelfId);

            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return result == DBNull.Value || result == null ? 0m : Convert.ToDecimal(result);
        }

        public async Task<decimal> GetShelfAvailableCapacityAsync(int shelfId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT dbo.fn_GetShelfAvailableCapacity(@ShelfID)", connection);
            command.Parameters.AddWithValue("@ShelfID", shelfId);

            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return result == DBNull.Value || result == null ? 0m : Convert.ToDecimal(result);
        }

        public async Task<decimal> GetShelfAvailableWeightCapacityAsync(int shelfId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT dbo.fn_GetShelfAvailableWeightCapacity(@ShelfID)", connection);

            command.Parameters.AddWithValue("@ShelfID", shelfId);

            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return result == DBNull.Value || result == null ? 0m : Convert.ToDecimal(result);
        }

        public async Task<decimal> GetRequiredWeightAsync(int productId, decimal quantity)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT ISNULL(WeightKg, 0) * @Quantity FROM Products WHERE ProductID = @ProductID", connection);
            command.Parameters.AddWithValue("@ProductID", productId);
            command.Parameters.AddWithValue("@Quantity", quantity);

            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return result == DBNull.Value || result == null ? 0m : Convert.ToDecimal(result);
        }

        public async Task<bool> IsShelfCompatibleForProductAsync(int productId, int shelfId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT CASE
                    WHEN s.ShelfType = 'QC Hold' THEN 0
                    WHEN EXISTS (
                        SELECT 1
                        FROM ProductStorageRules psr
                        INNER JOIN Racks rk2 ON s.RackID = rk2.RackID
                        INNER JOIN Rooms rm2 ON rk2.RoomID = rm2.RoomID
                        INNER JOIN Floors f2 ON rm2.FloorID = f2.FloorID
                        WHERE psr.ProductID = @ProductID
                          AND psr.IsActive = 1
                          AND (
                              psr.AllowedShelfType = s.ShelfType
                              OR psr.PreferredShelfID = s.ShelfID
                              OR psr.PreferredRackID = s.RackID
                              OR psr.PreferredWarehouseID = f2.WarehouseID
                          )
                    ) THEN 1
                    WHEN NOT EXISTS (SELECT 1 FROM ProductStorageRules WHERE ProductID = @ProductID AND IsActive = 1)
                         AND s.ShelfType = (
                            SELECT CASE ProductType
                                WHEN 'Raw Material' THEN 'Raw Material'
                                WHEN 'Finished Goods' THEN 'Finished Goods'
                                WHEN 'Consumable' THEN 'General'
                                ELSE 'General'
                            END
                            FROM Products
                            WHERE ProductID = @ProductID
                         ) THEN 1
                    ELSE 0
                END
                FROM Shelves s
                WHERE s.ShelfID = @ShelfID AND s.IsActive = 1", connection);

            command.Parameters.AddWithValue("@ProductID", productId);
            command.Parameters.AddWithValue("@ShelfID", shelfId);

            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return result != null && result != DBNull.Value && Convert.ToInt32(result) == 1;
        }

        public async Task<string> DeriveTransferTypeAsync(int fromShelfId, int toShelfId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT CASE
                    WHEN fw.WarehouseID <> tw.WarehouseID THEN 'Inter-Warehouse'
                    WHEN ff.FloorID <> tf.FloorID THEN 'Inter-Floor'
                    WHEN frm.RoomID <> trm.RoomID THEN 'Inter-Room'
                    WHEN fr.RackID <> tr.RackID THEN 'Inter-Rack'
                    ELSE 'Inter-Shelf'
                END
                FROM Shelves fs
                INNER JOIN Racks fr ON fs.RackID = fr.RackID
                INNER JOIN Rooms frm ON fr.RoomID = frm.RoomID
                INNER JOIN Floors ff ON frm.FloorID = ff.FloorID
                INNER JOIN Warehouses fw ON ff.WarehouseID = fw.WarehouseID
                INNER JOIN Shelves ts ON ts.ShelfID = @ToShelfID
                INNER JOIN Racks tr ON ts.RackID = tr.RackID
                INNER JOIN Rooms trm ON tr.RoomID = trm.RoomID
                INNER JOIN Floors tf ON trm.FloorID = tf.FloorID
                INNER JOIN Warehouses tw ON tf.WarehouseID = tw.WarehouseID
                WHERE fs.ShelfID = @FromShelfID", connection);

            command.Parameters.AddWithValue("@FromShelfID", fromShelfId);
            command.Parameters.AddWithValue("@ToShelfID", toShelfId);

            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return result?.ToString() ?? "Inter-Shelf";
        }

        public async Task<string> GetNextTransferNoAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqlCommand("SELECT 'TRF-' + CONVERT(NVARCHAR(8), GETDATE(), 112) + '-' + RIGHT('0000' + CAST(COUNT(*) + 1 AS NVARCHAR(4)), 4) FROM Transfers WHERE CONVERT(DATE, RequestDate) = CONVERT(DATE, GETDATE())", connection);
            var result = await command.ExecuteScalarAsync();
            return result?.ToString() ?? $"TRF-{DateTime.UtcNow:yyyyMMdd}-0001";
        }

        public async Task<IEnumerable<dynamic>> GetActiveProductsAsync()
        {
            var results = new List<dynamic>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT ProductID, SKU, ProductName FROM Products WHERE Status = 'Active' ORDER BY ProductName", connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new { ID = reader.GetInt32(0), SKU = reader.GetString(1), Name = reader.GetString(2) });
            }
            return results;
        }

        public async Task<IEnumerable<dynamic>> GetActiveShelvesAsync()
        {
            var results = new List<dynamic>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT s.ShelfID, w.WarehouseName + ' -> ' + f.FloorName + ' -> ' + rm.RoomName + ' -> ' + rk.RackName + ' -> ' + s.ShelfCode + ' [' + s.ShelfType + ']' AS FullPath
                FROM Shelves s
                INNER JOIN Racks rk ON s.RackID = rk.RackID
                INNER JOIN Rooms rm ON rk.RoomID = rm.RoomID
                INNER JOIN Floors f ON rm.FloorID = f.FloorID
                INNER JOIN Warehouses w ON f.WarehouseID = w.WarehouseID
                WHERE s.IsActive = 1
                ORDER BY w.WarehouseName, s.ShelfCode", connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new { ID = reader.GetInt32(0), Name = reader.GetString(1) });
            }
            return results;
        }

        public async Task<IEnumerable<dynamic>> GetShelvesWithProductAsync(int productId)
        {
            var results = new List<dynamic>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT s.ShelfID, w.WarehouseName + ' -> ' + s.ShelfCode + ' (Available: ' + CAST(dbo.fn_GetAvailableStock(@ProductID, s.ShelfID) AS NVARCHAR(50)) + ')' AS FullPath
                FROM InventoryStocks ist
                INNER JOIN Shelves s ON ist.ShelfID = s.ShelfID
                INNER JOIN Racks rk ON s.RackID = rk.RackID
                INNER JOIN Rooms rm ON rk.RoomID = rm.RoomID
                INNER JOIN Floors f ON rm.FloorID = f.FloorID
                INNER JOIN Warehouses w ON f.WarehouseID = w.WarehouseID
                WHERE s.IsActive = 1 AND ist.ProductID = @ProductID AND dbo.fn_GetAvailableStock(@ProductID, s.ShelfID) > 0
                ORDER BY w.WarehouseName, s.ShelfCode", connection);
            command.Parameters.AddWithValue("@ProductID", productId);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new { ID = reader.GetInt32(0), Name = reader.GetString(1) });
            }
            return results;
        }

        public async Task<IEnumerable<dynamic>> GetProductsByShelfAsync(int shelfId)
        {
            var results = new List<dynamic>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT
                    p.ProductID,
                    '[' + p.SKU + '] ' + p.ProductName + ' (' + p.ProductType + ', Avail: ' + CAST(dbo.fn_GetAvailableStock(p.ProductID, @ShelfID) AS NVARCHAR(50)) + ')' AS DisplayName,
                    dbo.fn_GetAvailableStock(p.ProductID, @ShelfID) AS AvailableQty,
                    p.ProductType,
                    ISNULL((SELECT TOP 1 AllowedShelfType FROM ProductStorageRules WHERE ProductID = p.ProductID AND IsActive = 1),
                           CASE p.ProductType
                               WHEN 'Raw Material' THEN 'Raw Material'
                               WHEN 'Finished Goods' THEN 'Finished Goods'
                               WHEN 'Consumable' THEN 'General'
                               ELSE 'General'
                           END) AS RequiredShelfType
                FROM InventoryStocks ist
                INNER JOIN Products p ON ist.ProductID = p.ProductID
                WHERE ist.ShelfID = @ShelfID
                  AND p.Status = 'Active'
                  AND dbo.fn_GetAvailableStock(p.ProductID, @ShelfID) > 0
                ORDER BY p.SKU", connection);

            command.Parameters.AddWithValue("@ShelfID", shelfId);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new
                {
                    ID = reader.GetInt32("ProductID"),
                    Name = reader.GetString("DisplayName"),
                    AvailableQty = reader.GetDecimal("AvailableQty"),
                    ProductType = reader.GetString("ProductType"),
                    RequiredShelfType = reader.GetString("RequiredShelfType")
                });
            }
            return results;
        }

        public async Task<IEnumerable<ShelfOptionDto>> GetDestinationShelvesForProductAsync(int productId, decimal quantity)
        {
            var results = new List<ShelfOptionDto>();
            var requiredWeight = await GetRequiredWeightAsync(productId, quantity);

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT
                    s.ShelfID,
                    s.ShelfCode,
                    s.ShelfType,
                    w.WarehouseName + ' -> ' + f.FloorName + ' -> ' + rm.RoomName + ' -> ' + rk.RackName + ' -> ' + s.ShelfCode + ' [' + s.ShelfType + ']' AS FullPath,
                    dbo.fn_GetShelfAvailableCapacity(s.ShelfID) AS QuantityAvailable,
                    dbo.fn_GetShelfAvailableWeightCapacity(s.ShelfID) AS WeightAvailableKg,
                    CASE WHEN s.MaxQty > 0 THEN ((s.MaxQty - dbo.fn_GetShelfAvailableCapacity(s.ShelfID)) / s.MaxQty) * 100 ELSE 0 END AS UtilizationPercentage,
                    CASE
                        WHEN EXISTS (
                            SELECT 1
                            FROM ProductStorageRules psr
                            WHERE psr.ProductID = @ProductID
                              AND psr.IsActive = 1
                              AND (
                                  psr.AllowedShelfType = s.ShelfType
                                  OR psr.PreferredShelfID = s.ShelfID
                                  OR psr.PreferredRackID = rk.RackID
                                  OR psr.PreferredWarehouseID = w.WarehouseID
                              )
                        ) THEN 1
                        WHEN NOT EXISTS (SELECT 1 FROM ProductStorageRules psr WHERE psr.ProductID = @ProductID AND psr.IsActive = 1)
                             AND s.ShelfType IN (
                                SELECT CASE ProductType
                                    WHEN 'Raw Material' THEN 'Raw Material'
                                    WHEN 'Finished Goods' THEN 'Finished Goods'
                                    WHEN 'Consumable' THEN 'General'
                                    ELSE 'General'
                                END
                                FROM Products
                                WHERE ProductID = @ProductID
                             ) THEN 1
                        ELSE 0
                    END AS IsCompatible
                FROM Shelves s
                INNER JOIN Racks rk ON s.RackID = rk.RackID
                INNER JOIN Rooms rm ON rk.RoomID = rm.RoomID
                INNER JOIN Floors f ON rm.FloorID = f.FloorID
                INNER JOIN Warehouses w ON f.WarehouseID = w.WarehouseID
                WHERE s.IsActive = 1
                  AND s.ShelfType <> 'QC Hold'
                ORDER BY
                    CASE WHEN dbo.fn_GetShelfAvailableCapacity(s.ShelfID) >= @Quantity
                           AND dbo.fn_GetShelfAvailableWeightCapacity(s.ShelfID) >= @RequiredWeight THEN 0 ELSE 1 END,
                    IsCompatible DESC,
                    UtilizationPercentage ASC,
                    w.WarehouseName,
                    s.ShelfCode", connection);

            command.Parameters.AddWithValue("@ProductID", productId);
            command.Parameters.AddWithValue("@Quantity", quantity);
            command.Parameters.AddWithValue("@RequiredWeight", requiredWeight);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var quantityAvailable = reader.GetDecimal("QuantityAvailable");
                var weightAvailable = reader.GetDecimal("WeightAvailableKg");
                var isCompatible = reader.GetInt32("IsCompatible") == 1;
                var hasCapacity = quantityAvailable >= quantity && weightAvailable >= requiredWeight;

                results.Add(new ShelfOptionDto
                {
                    ID = reader.GetInt32("ShelfID"),
                    Name = reader.GetString("FullPath"),
                    ShelfCode = reader.GetString("ShelfCode"),
                    ShelfType = reader.GetString("ShelfType"),
                    QuantityAvailable = quantityAvailable,
                    WeightAvailableKg = weightAvailable,
                    UtilizationPercentage = reader.GetDecimal("UtilizationPercentage"),
                    IsCompatible = isCompatible,
                    IsRecommended = isCompatible && hasCapacity,
                    Warning = isCompatible
                        ? hasCapacity ? "" : "Capacity too low"
                        : "Shelf type not allowed"
                });
            }

            return results;
        }

        public async Task<ShelfCapacityInfoDto?> GetShelfCapacityInfoAsync(int shelfId, int? productId = null)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT 
                    s.ShelfID,
                    s.ShelfCode,
                    ISNULL(s.ShelfName, s.ShelfCode) AS ShelfName,
                    s.ShelfType,
                    ISNULL(s.MaxQty, 0) AS MaxQty,
                    ISNULL(s.MaxWeightKg, 0) AS MaxWeightKg,
                    ISNULL(SUM(ist.CurrentQty), 0) AS CurrentQty,
                    dbo.fn_GetShelfAvailableCapacity(s.ShelfID) AS AvailableQty,
                    dbo.fn_GetShelfAvailableWeightCapacity(s.ShelfID) AS AvailableWeightKg,
                    w.WarehouseName,
                    f.FloorName,
                    rm.RoomName,
                    rk.RackName
                FROM Shelves s
                INNER JOIN Racks rk ON s.RackID = rk.RackID
                INNER JOIN Rooms rm ON rk.RoomID = rm.RoomID
                INNER JOIN Floors f ON rm.FloorID = f.FloorID
                INNER JOIN Warehouses w ON f.WarehouseID = w.WarehouseID
                LEFT JOIN InventoryStocks ist ON s.ShelfID = ist.ShelfID
                WHERE s.ShelfID = @ShelfID
                GROUP BY s.ShelfID, s.ShelfCode, s.ShelfName, s.ShelfType, s.MaxQty, s.MaxWeightKg, w.WarehouseName, f.FloorName, rm.RoomName, rk.RackName", connection);

            command.Parameters.AddWithValue("@ShelfID", shelfId);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                decimal maxQty = reader.GetDecimal("MaxQty");
                decimal currentQty = reader.GetDecimal("CurrentQty");
                decimal availableQty = reader.GetDecimal("AvailableQty");
                decimal maxWeight = reader.GetDecimal("MaxWeightKg");
                decimal availableWeight = reader.GetDecimal("AvailableWeightKg");
                decimal utilizationPct = maxQty > 0 ? ((maxQty - availableQty) / maxQty) * 100 : 0;
                string shelfType = reader.GetString("ShelfType");

                var info = new ShelfCapacityInfoDto
                {
                    ShelfID = reader.GetInt32("ShelfID"),
                    ShelfCode = reader.GetString("ShelfCode"),
                    ShelfName = reader.GetString("ShelfName"),
                    ShelfType = shelfType,
                    MaxQty = maxQty,
                    MaxWeightKg = maxWeight,
                    CurrentQty = currentQty,
                    AvailableQty = availableQty,
                    AvailableWeightKg = availableWeight,
                    UtilizationPercentage = utilizationPct,
                    FullPath = $"{reader.GetString("WarehouseName")} > {reader.GetString("FloorName")} > {reader.GetString("RoomName")} > {reader.GetString("RackName")} > {reader.GetString("ShelfCode")}"
                };

                if (productId.HasValue && productId.Value > 0)
                {
                    bool isCompatible = await IsShelfCompatibleForProductAsync(productId.Value, shelfId);
                    info.IsCompatible = isCompatible;
                    if (!isCompatible)
                    {
                        info.IncompatibilityReason = $"This shelf type [{shelfType}] is not allowed for this product. Please select a matching shelf.";
                    }
                }

                return info;
            }
            return null;
        }
    }
}

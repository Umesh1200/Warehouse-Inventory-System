using IMS.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public class InventoryAdjustmentRepository : IInventoryAdjustmentRepository
    {
        private readonly string _connectionString;

        public InventoryAdjustmentRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<IEnumerable<InventoryAdjustmentDto>> GetAllAdjustmentsAsync()
        {
            var results = new List<InventoryAdjustmentDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT adj.*, 
                           ur.FullName AS RequestedByName, 
                           ua.FullName AS ApprovedByName,
                           items.ItemCount,
                           items.TotalDifferenceQty
                    FROM Adjustments adj
                    LEFT JOIN Users ur ON adj.RequestedBy = ur.UserID
                    LEFT JOIN Users ua ON adj.ApprovedBy = ua.UserID
                    LEFT JOIN (
                        SELECT AdjustmentID, 
                               COUNT(*) AS ItemCount, 
                               SUM(CountedQty - SystemQty) AS TotalDifferenceQty
                        FROM AdjustmentItems
                        GROUP BY AdjustmentID
                    ) items ON adj.AdjustmentID = items.AdjustmentID
                    ORDER BY adj.RequestDate DESC", connection);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new InventoryAdjustmentDto
                    {
                        AdjustmentID = reader.GetInt32("AdjustmentID"),
                        AdjustmentNo = reader.GetString("AdjustmentNo"),
                        AdjustmentType = reader.GetString("AdjustmentType"),
                        Reason = reader.IsDBNull("Reason") ? "" : reader.GetString("Reason"),
                        RequestedBy = reader.GetInt32("RequestedBy"),
                        RequestedByName = reader.IsDBNull("RequestedByName") ? "" : reader.GetString("RequestedByName"),
                        ApprovedBy = reader.IsDBNull("ApprovedBy") ? (int?)null : reader.GetInt32("ApprovedBy"),
                        ApprovedByName = reader.IsDBNull("ApprovedByName") ? "" : reader.GetString("ApprovedByName"),
                        Status = reader.GetString("Status"),
                        RequestDate = reader.GetDateTime("RequestDate"),
                        ItemCount = reader.IsDBNull("ItemCount") ? 0 : reader.GetInt32("ItemCount"),
                        TotalDifferenceQty = reader.IsDBNull("TotalDifferenceQty") ? 0 : reader.GetDecimal("TotalDifferenceQty")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching inventory adjustments.", ex);
            }

            return results;
        }

        public async Task<IEnumerable<InventoryAdjustmentItemDto>> GetAdjustmentItemsAsync(int adjustmentId)
        {
            var results = new List<InventoryAdjustmentItemDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT ai.*, 
                           p.SKU, p.ProductName,
                           s.ShelfCode, 
                           w.WarehouseName, 
                           f.FloorName, 
                           rm.RoomName, 
                           rk.RackName
                    FROM AdjustmentItems ai
                    INNER JOIN Products p ON ai.ProductID = p.ProductID
                    INNER JOIN Shelves s ON ai.ShelfID = s.ShelfID
                    INNER JOIN Racks rk ON s.RackID = rk.RackID
                    INNER JOIN Rooms rm ON rk.RoomID = rm.RoomID
                    INNER JOIN Floors f ON rm.FloorID = f.FloorID
                    INNER JOIN Warehouses w ON f.WarehouseID = w.WarehouseID
                    WHERE ai.AdjustmentID = @AdjustmentID", connection);

                command.Parameters.AddWithValue("@AdjustmentID", adjustmentId);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new InventoryAdjustmentItemDto
                    {
                        AdjItemID = reader.GetInt32("AdjItemID"),
                        AdjustmentID = reader.GetInt32("AdjustmentID"),
                        ProductID = reader.GetInt32("ProductID"),
                        SKU = reader.GetString("SKU"),
                        ProductName = reader.GetString("ProductName"),
                        ShelfID = reader.GetInt32("ShelfID"),
                        ShelfCode = reader.GetString("ShelfCode"),
                        WarehouseName = reader.GetString("WarehouseName"),
                        FloorName = reader.IsDBNull("FloorName") ? "" : reader.GetString("FloorName"),
                        RoomName = reader.IsDBNull("RoomName") ? "" : reader.GetString("RoomName"),
                        RackName = reader.IsDBNull("RackName") ? "" : reader.GetString("RackName"),
                        SystemQty = reader.GetDecimal("SystemQty"),
                        CountedQty = reader.GetDecimal("CountedQty"),
                        DifferenceQty = reader.GetDecimal("DifferenceQty")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching adjustment items.", ex);
            }

            return results;
        }

        public async Task<int> CreateAdjustmentWithItemsAsync(AdjustmentCreateDto adjustment)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                if (string.IsNullOrWhiteSpace(adjustment.AdjustmentNo))
                {
                    adjustment.AdjustmentNo = $"ADJ-{DateTime.Now:yyyyMMddHHmmss}-{new Random().Next(100, 999)}";
                }
                else
                {
                    // Check if AdjustmentNo already exists to prevent duplicate key constraint error
                    using var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Adjustments WHERE AdjustmentNo = @No", connection, transaction);
                    checkCmd.Parameters.AddWithValue("@No", adjustment.AdjustmentNo);
                    int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                    if (count > 0)
                    {
                        adjustment.AdjustmentNo = $"{adjustment.AdjustmentNo}-{DateTime.Now:HHmmss}";
                    }
                }

                // 1. Insert Header
                using var cmdHeader = new SqlCommand(@"
                    INSERT INTO Adjustments (AdjustmentNo, AdjustmentType, Reason, RequestedBy, Status, RequestDate)
                    VALUES (@AdjustmentNo, @AdjustmentType, @Reason, @RequestedBy, 'Awaiting Approval', GETDATE());
                    SELECT SCOPE_IDENTITY();", connection, transaction);

                cmdHeader.Parameters.AddWithValue("@AdjustmentNo", adjustment.AdjustmentNo);
                cmdHeader.Parameters.AddWithValue("@AdjustmentType", adjustment.AdjustmentType);
                cmdHeader.Parameters.AddWithValue("@Reason", adjustment.Reason ?? "");
                cmdHeader.Parameters.AddWithValue("@RequestedBy", adjustment.RequestedBy > 0 ? adjustment.RequestedBy : 1);

                var adjustmentId = Convert.ToInt32(await cmdHeader.ExecuteScalarAsync());

                // 2. Insert Items (Note: DifferenceQty is a COMPUTED column in database so we do not insert it)
                foreach (var item in adjustment.Items)
                {
                    using var cmdItem = new SqlCommand(@"
                        INSERT INTO AdjustmentItems (AdjustmentID, ProductID, ShelfID, SystemQty, CountedQty)
                        VALUES (@AdjustmentID, @ProductID, @ShelfID, @SystemQty, @CountedQty)", 
                        connection, transaction);

                    cmdItem.Parameters.AddWithValue("@AdjustmentID", adjustmentId);
                    cmdItem.Parameters.AddWithValue("@ProductID", item.ProductID > 0 ? item.ProductID : 1);
                    cmdItem.Parameters.AddWithValue("@ShelfID", item.ShelfID > 0 ? item.ShelfID : 1);
                    cmdItem.Parameters.AddWithValue("@SystemQty", item.SystemQty);
                    cmdItem.Parameters.AddWithValue("@CountedQty", item.CountedQty);

                    await cmdItem.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return adjustmentId;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine("Error in CreateAdjustmentWithItemsAsync: " + ex.ToString());
                throw new Exception("Error creating adjustment request: " + ex.Message, ex);
            }
        }

        public async Task<IEnumerable<InventoryAdjustmentRowDto>> GetAllAdjustmentRowsAsync()
        {
            var results = new List<InventoryAdjustmentRowDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT adj.AdjustmentID,
                           adj.AdjustmentNo,
                           adj.AdjustmentType,
                           adj.Reason,
                           adj.RequestedBy,
                           ur.FullName AS RequestedByName,
                           adj.ApprovedBy,
                           ua.FullName AS ApprovedByName,
                           adj.Status,
                           adj.RequestDate,
                           ISNULL(ai.ProductID, 0) AS ProductID,
                           ISNULL(p.SKU, '-') AS SKU,
                           ISNULL(p.ProductName, 'Multiple Items') AS ProductName,
                           ISNULL(ai.ShelfID, 0) AS ShelfID,
                           ISNULL(s.ShelfCode, '-') AS ShelfCode,
                           ISNULL(w.WarehouseName, 'Various') AS WarehouseName,
                           ISNULL(ai.SystemQty, 0) AS SystemQty,
                           ISNULL(ai.CountedQty, 0) AS CountedQty,
                           ISNULL(ai.DifferenceQty, 0) AS DifferenceQty,
                           ISNULL(items.ItemCount, 0) AS ItemCount
                    FROM Adjustments adj
                    LEFT JOIN Users ur ON adj.RequestedBy = ur.UserID
                    LEFT JOIN Users ua ON adj.ApprovedBy = ua.UserID
                    LEFT JOIN (
                        SELECT AdjustmentID, 
                               COUNT(*) AS ItemCount, 
                               SUM(CountedQty - SystemQty) AS TotalDifferenceQty
                        FROM AdjustmentItems
                        GROUP BY AdjustmentID
                    ) items ON adj.AdjustmentID = items.AdjustmentID
                    OUTER APPLY (
                        SELECT TOP 1 *
                        FROM AdjustmentItems
                        WHERE AdjustmentID = adj.AdjustmentID
                        ORDER BY AdjItemID
                    ) ai
                    LEFT JOIN Products p ON ai.ProductID = p.ProductID
                    LEFT JOIN Shelves s ON ai.ShelfID = s.ShelfID
                    LEFT JOIN Racks rk ON s.RackID = rk.RackID
                    LEFT JOIN Rooms rm ON rk.RoomID = rm.RoomID
                    LEFT JOIN Floors f ON rm.FloorID = f.FloorID
                    LEFT JOIN Warehouses w ON f.WarehouseID = w.WarehouseID
                    ORDER BY adj.RequestDate DESC, adj.AdjustmentID DESC", connection);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new InventoryAdjustmentRowDto
                    {
                        AdjustmentID = reader.GetInt32("AdjustmentID"),
                        AdjustmentNo = reader.IsDBNull("AdjustmentNo") ? "ADJ-UNKNOWN" : reader.GetString("AdjustmentNo"),
                        AdjustmentType = reader.IsDBNull("AdjustmentType") ? "AdjustmentIn" : reader.GetString("AdjustmentType"),
                        Reason = reader.IsDBNull("Reason") ? "" : reader.GetString("Reason"),
                        RequestedBy = reader.IsDBNull("RequestedBy") ? 1 : reader.GetInt32("RequestedBy"),
                        RequestedByName = reader.IsDBNull("RequestedByName") ? "System User" : reader.GetString("RequestedByName"),
                        ApprovedBy = reader.IsDBNull("ApprovedBy") ? (int?)null : reader.GetInt32("ApprovedBy"),
                        ApprovedByName = reader.IsDBNull("ApprovedByName") ? "Pending" : reader.GetString("ApprovedByName"),
                        Status = reader.IsDBNull("Status") ? "Pending" : reader.GetString("Status"),
                        RequestDate = reader.IsDBNull("RequestDate") ? DateTime.Now : reader.GetDateTime("RequestDate"),
                        ProductID = reader.IsDBNull("ProductID") ? 0 : reader.GetInt32("ProductID"),
                        SKU = reader.IsDBNull("SKU") ? "-" : reader.GetString("SKU"),
                        ProductName = reader.IsDBNull("ProductName") ? "Multiple Items" : reader.GetString("ProductName"),
                        ShelfID = reader.IsDBNull("ShelfID") ? 0 : reader.GetInt32("ShelfID"),
                        ShelfCode = reader.IsDBNull("ShelfCode") ? "-" : reader.GetString("ShelfCode"),
                        WarehouseName = reader.IsDBNull("WarehouseName") ? "Various" : reader.GetString("WarehouseName"),
                        SystemQty = reader.IsDBNull("SystemQty") ? 0m : reader.GetDecimal("SystemQty"),
                        CountedQty = reader.IsDBNull("CountedQty") ? 0m : reader.GetDecimal("CountedQty"),
                        DifferenceQty = reader.IsDBNull("DifferenceQty") ? 0m : reader.GetDecimal("DifferenceQty"),
                        ItemCount = reader.IsDBNull("ItemCount") ? 0 : reader.GetInt32("ItemCount")
                    });
                }
            }
            catch (Exception ex)
            {
                // Return whatever we have or empty list instead of failing page
                Console.WriteLine("Error in GetAllAdjustmentRowsAsync: " + ex.Message);
            }

            return results;
        }

        public async Task<InventoryAdjustmentDto?> GetAdjustmentByIdAsync(int adjustmentId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT adj.*, 
                           ur.FullName AS RequestedByName, 
                           ua.FullName AS ApprovedByName
                    FROM Adjustments adj
                    LEFT JOIN Users ur ON adj.RequestedBy = ur.UserID
                    LEFT JOIN Users ua ON adj.ApprovedBy = ua.UserID
                    WHERE adj.AdjustmentID = @AdjustmentID", connection);
                command.Parameters.AddWithValue("@AdjustmentID", adjustmentId);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new InventoryAdjustmentDto
                    {
                        AdjustmentID = reader.GetInt32("AdjustmentID"),
                        AdjustmentNo = reader.IsDBNull("AdjustmentNo") ? "ADJ-UNKNOWN" : reader.GetString("AdjustmentNo"),
                        AdjustmentType = reader.IsDBNull("AdjustmentType") ? "AdjustmentIn" : reader.GetString("AdjustmentType"),
                        Reason = reader.IsDBNull("Reason") ? "" : reader.GetString("Reason"),
                        RequestedBy = reader.IsDBNull("RequestedBy") ? 1 : reader.GetInt32("RequestedBy"),
                        RequestedByName = reader.IsDBNull("RequestedByName") ? "System User" : reader.GetString("RequestedByName"),
                        ApprovedBy = reader.IsDBNull("ApprovedBy") ? (int?)null : reader.GetInt32("ApprovedBy"),
                        ApprovedByName = reader.IsDBNull("ApprovedByName") ? "Pending" : reader.GetString("ApprovedByName"),
                        Status = reader.IsDBNull("Status") ? "Pending" : reader.GetString("Status"),
                        RequestDate = reader.IsDBNull("RequestDate") ? DateTime.Now : reader.GetDateTime("RequestDate")
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetAdjustmentByIdAsync: " + ex.Message);
                return null;
            }
        }

        public async Task<bool> ApproveAdjustmentAsync(int adjustmentId, int approvedBy)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    UPDATE Adjustments 
                    SET Status = 'Approved', ApprovedBy = @ApprovedBy
                    WHERE AdjustmentID = @AdjustmentID AND (Status = 'Pending' OR Status = 'Awaiting Approval')", connection);

                command.Parameters.AddWithValue("@AdjustmentID", adjustmentId);
                command.Parameters.AddWithValue("@ApprovedBy", approvedBy);

                await connection.OpenAsync();
                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error approving adjustment.", ex);
            }
        }

        public async Task<AdjustmentActionResultDto> PostAdjustmentAsync(int adjustmentId, int performedBy)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand("dbo.sp_PostAdjustment", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@AdjustmentID", adjustmentId);
                command.Parameters.AddWithValue("@PerformedBy", performedBy);

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();

                return new AdjustmentActionResultDto { Success = true, Message = "Adjustment posted successfully. Stock updated." };
            }
            catch (SqlException ex)
            {
                return new AdjustmentActionResultDto { Success = false, Message = ex.Message };
            }
            catch (Exception ex)
            {
                return new AdjustmentActionResultDto { Success = false, Message = "An unexpected error occurred during posting. " + ex.Message };
            }
        }

        public async Task<decimal> GetCurrentStockAsync(int productId, int shelfId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT ISNULL(SUM(CurrentQty), 0) 
                FROM InventoryStocks 
                WHERE ProductID = @ProductID AND ShelfID = @ShelfID", connection);

            command.Parameters.AddWithValue("@ProductID", productId);
            command.Parameters.AddWithValue("@ShelfID", shelfId);

            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return result == DBNull.Value || result == null ? 0m : Convert.ToDecimal(result);
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
                SELECT s.ShelfID, w.WarehouseName + ' -> ' + f.FloorName + ' -> ' + rm.RoomName + ' -> ' + rk.RackName + ' -> ' + s.ShelfCode AS FullPath
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

        public async Task<IEnumerable<string>> GetWarehousesAsync()
        {
            var results = new List<string>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT WarehouseName FROM Warehouses WHERE IsActive = 1 ORDER BY WarehouseName", connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(reader.GetString(0));
            }
            return results;
        }
    }
}

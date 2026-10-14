using IMS.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public class MaterialIssueReturnRepository : IMaterialIssueReturnRepository
    {
        private readonly string _connectionString;

        public MaterialIssueReturnRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<IEnumerable<MaterialIssueReturnDto>> GetIssueReturnTasksAsync()
        {
            var results = new List<MaterialIssueReturnDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT 
                        pmi.IssueID, pmi.PlanID, pp.PlanNumber, pmi.ProductID, p.SKU, p.ProductName,
                        pmi.ShelfID, s.ShelfCode, w.WarehouseName, f.FloorName, rm.RoomName, r.RackName,
                        pmi.RequestedQty, pmi.IssuedQty, pmi.ReturnedQty, pmi.Status,
                        pmi.IssuedBy, u.FullName AS IssuedByName, pmi.IssueDate,
                        dbo.fn_GetAvailableStock(pmi.ProductID, pmi.ShelfID) AS AvailableQty
                    FROM ProductionMaterialIssues pmi
                    INNER JOIN ProductionPlans pp ON pmi.PlanID = pp.PlanID
                    INNER JOIN Products p ON pmi.ProductID = p.ProductID
                    INNER JOIN Shelves s ON pmi.ShelfID = s.ShelfID
                    INNER JOIN Racks r ON s.RackID = r.RackID
                    INNER JOIN Rooms rm ON r.RoomID = rm.RoomID
                    INNER JOIN Floors f ON rm.FloorID = f.FloorID
                    INNER JOIN Warehouses w ON f.WarehouseID = w.WarehouseID
                    LEFT JOIN Users u ON pmi.IssuedBy = u.UserID
                    ORDER BY pmi.IssueDate DESC", connection);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new MaterialIssueReturnDto
                    {
                        IssueID = reader.GetInt32("IssueID"),
                        PlanID = reader.GetInt32("PlanID"),
                        PlanNumber = reader.GetString("PlanNumber"),
                        ProductID = reader.GetInt32("ProductID"),
                        SKU = reader.GetString("SKU"),
                        ProductName = reader.GetString("ProductName"),
                        ShelfID = reader.GetInt32("ShelfID"),
                        ShelfCode = reader.GetString("ShelfCode"),
                        WarehouseName = reader.GetString("WarehouseName"),
                        FloorName = reader.IsDBNull("FloorName") ? "" : reader.GetString("FloorName"),
                        RoomName = reader.IsDBNull("RoomName") ? "" : reader.GetString("RoomName"),
                        RackName = reader.IsDBNull("RackName") ? "" : reader.GetString("RackName"),
                        RequestedQty = reader.GetDecimal("RequestedQty"),
                        IssuedQty = reader.GetDecimal("IssuedQty"),
                        ReturnedQty = reader.GetDecimal("ReturnedQty"),
                        Status = reader.GetString("Status"),
                        IssuedBy = reader.IsDBNull("IssuedBy") ? (int?)null : reader.GetInt32("IssuedBy"),
                        IssuedByName = reader.IsDBNull("IssuedByName") ? "" : reader.GetString("IssuedByName"),
                        IssueDate = reader.GetDateTime("IssueDate"),
                        AvailableQty = reader.GetDecimal("AvailableQty")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching material issue tasks.", ex);
            }

            return results;
        }

        public async Task<int> CreateIssueRequestAsync(MaterialIssueCreateDto issue)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    INSERT INTO ProductionMaterialIssues (PlanID, ProductID, ShelfID, RequestedQty, IssuedQty, ReturnedQty, Status, IssuedBy, IssueDate)
                    VALUES (@PlanID, @ProductID, @ShelfID, @RequestedQty, 0, 0, 'Pending', @IssuedBy, GETDATE());
                    SELECT SCOPE_IDENTITY();", connection);

                command.Parameters.AddWithValue("@PlanID", issue.PlanID);
                command.Parameters.AddWithValue("@ProductID", issue.ProductID);
                command.Parameters.AddWithValue("@ShelfID", issue.ShelfID);
                command.Parameters.AddWithValue("@RequestedQty", issue.RequestedQty);
                command.Parameters.AddWithValue("@IssuedBy", issue.IssuedBy);

                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating material issue request.", ex);
            }
        }

        public async Task<MaterialIssueReturnActionResultDto> IssueMaterialAsync(MaterialIssueConfirmDto issue)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand("dbo.sp_IssueProductionMaterial", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@IssueID", issue.IssueID);
                command.Parameters.AddWithValue("@PerformedBy", issue.PerformedBy);

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();

                return new MaterialIssueReturnActionResultDto { Success = true, Message = "Material issued successfully. Stock reduced." };
            }
            catch (SqlException ex)
            {
                return new MaterialIssueReturnActionResultDto { Success = false, Message = ex.Message };
            }
            catch (Exception ex)
            {
                return new MaterialIssueReturnActionResultDto { Success = false, Message = "An unexpected error occurred during material issue. " + ex.Message };
            }
        }

        public async Task<MaterialIssueReturnActionResultDto> ReturnMaterialAsync(MaterialReturnDto returnDto)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Get Plan context for audit
                using var cmdPlan = new SqlCommand(@"
                    SELECT pp.PlanNumber, pmi.ProductID
                    FROM ProductionMaterialIssues pmi
                    INNER JOIN ProductionPlans pp ON pmi.PlanID = pp.PlanID
                    WHERE pmi.IssueID = @IssueID", connection, transaction);
                cmdPlan.Parameters.AddWithValue("@IssueID", returnDto.IssueID);
                
                string planNo = "";
                int productId = 0;
                using (var reader = await cmdPlan.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        planNo = reader.GetString(0);
                        productId = reader.GetInt32(1);
                    }
                    else
                    {
                        throw new Exception("Issue record not found.");
                    }
                }

                // 2. Update Stock
                using var cmdStock = new SqlCommand(@"
                    IF EXISTS (SELECT 1 FROM InventoryStocks WHERE ProductID = @ProductID AND ShelfID = @ShelfID)
                        UPDATE InventoryStocks SET CurrentQty = CurrentQty + @Qty, LastUpdated = GETDATE() WHERE ProductID = @ProductID AND ShelfID = @ShelfID
                    ELSE
                        INSERT INTO InventoryStocks (ProductID, ShelfID, CurrentQty, ReservedQty, QCHoldQty, DamagedQty, LastUpdated)
                        VALUES (@ProductID, @ShelfID, @Qty, 0, 0, 0, GETDATE());", connection, transaction);
                
                cmdStock.Parameters.AddWithValue("@ProductID", productId);
                cmdStock.Parameters.AddWithValue("@ShelfID", returnDto.ShelfID);
                cmdStock.Parameters.AddWithValue("@Qty", returnDto.ReturnedQty);
                await cmdStock.ExecuteNonQueryAsync();

                // 3. Update Issue Record
                using var cmdUpdate = new SqlCommand(@"
                    UPDATE ProductionMaterialIssues 
                    SET ReturnedQty = ReturnedQty + @Qty
                    WHERE IssueID = @IssueID", connection, transaction);
                
                cmdUpdate.Parameters.AddWithValue("@Qty", returnDto.ReturnedQty);
                cmdUpdate.Parameters.AddWithValue("@IssueID", returnDto.IssueID);
                await cmdUpdate.ExecuteNonQueryAsync();

                // 4. Log Movement
                using var cmdLog = new SqlCommand(@"
                    INSERT INTO StockMovements (ProductID, ShelfID, MovementType, Quantity, ReferenceType, ReferenceNo, PerformedBy, CreatedAt, Remarks)
                    VALUES (@ProductID, @ShelfID, 'ProductionReturn', @Qty, 'ProductionPlan', @PlanNo, @PerformedBy, GETDATE(), 'Material returned from production');", connection, transaction);
                
                cmdLog.Parameters.AddWithValue("@ProductID", productId);
                cmdLog.Parameters.AddWithValue("@ShelfID", returnDto.ShelfID);
                cmdLog.Parameters.AddWithValue("@Qty", returnDto.ReturnedQty);
                cmdLog.Parameters.AddWithValue("@PlanNo", planNo);
                cmdLog.Parameters.AddWithValue("@PerformedBy", returnDto.ReturnedBy);
                await cmdLog.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
                return new MaterialIssueReturnActionResultDto { Success = true, Message = "Material return recorded. Stock added back." };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new MaterialIssueReturnActionResultDto { Success = false, Message = "Error recording return: " + ex.Message };
            }
        }

        public async Task<MaterialIssueReturnDto?> GetIssueByIdAsync(int issueId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand("SELECT * FROM ProductionMaterialIssues WHERE IssueID = @IssueID", connection);
                command.Parameters.AddWithValue("@IssueID", issueId);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new MaterialIssueReturnDto
                    {
                        IssueID = reader.GetInt32("IssueID"),
                        PlanID = reader.GetInt32("PlanID"),
                        ProductID = reader.GetInt32("ProductID"),
                        ShelfID = reader.GetInt32("ShelfID"),
                        RequestedQty = reader.GetDecimal("RequestedQty"),
                        IssuedQty = reader.GetDecimal("IssuedQty"),
                        ReturnedQty = reader.GetDecimal("ReturnedQty"),
                        Status = reader.GetString("Status")
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching issue details.", ex);
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

        public async Task<IEnumerable<dynamic>> GetActiveProductionPlansAsync()
        {
            var results = new List<dynamic>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT PlanID, PlanNumber FROM ProductionPlans WHERE Status IN ('Approved', 'In Production') ORDER BY PlanNumber", connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new { ID = reader.GetInt32(0), Number = reader.GetString(1) });
            }
            return results;
        }

        public async Task<IEnumerable<dynamic>> GetRawMaterialsAsync()
        {
            var results = new List<dynamic>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT ProductID, SKU, ProductName FROM Products WHERE Status = 'Active' AND ProductType = 'Raw Material' ORDER BY ProductName", connection);
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

        public async Task<bool> AutoGenerateMaterialIssuesAsync(int planId, int performedBy)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(@"
                DECLARE @BOMID INT, @PlannedQty DECIMAL(18,2);

                SELECT @BOMID = BOMID, @PlannedQty = PlannedQty
                FROM ProductionPlans
                WHERE PlanID = @PlanID;

                IF @BOMID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductionMaterialIssues WHERE PlanID = @PlanID)
                BEGIN
                    INSERT INTO ProductionMaterialIssues (PlanID, ProductID, ShelfID, RequestedQty, IssuedQty, ReturnedQty, Status, IssuedBy, IssueDate)
                    SELECT 
                        @PlanID,
                        bi.RawMaterialID,
                        COALESCE(
                            (SELECT TOP 1 s.ShelfID 
                             FROM InventoryStocks inv
                             INNER JOIN Shelves s ON inv.ShelfID = s.ShelfID
                             WHERE inv.ProductID = bi.RawMaterialID AND inv.CurrentQty > 0 AND s.IsActive = 1
                             ORDER BY inv.CurrentQty DESC),
                            (SELECT TOP 1 ShelfID FROM Shelves WHERE IsActive = 1 AND ShelfType IN ('Raw Material', 'Standard', 'General') ORDER BY ShelfID),
                            (SELECT TOP 1 ShelfID FROM Shelves WHERE IsActive = 1 ORDER BY ShelfID)
                        ) AS ShelfID,
                        ROUND(bi.RequiredQty * @PlannedQty * (1.0 + (ISNULL(bi.WastagePercent, 0) / 100.0)), 2) AS RequestedQty,
                        0 AS IssuedQty,
                        0 AS ReturnedQty,
                        'Pending' AS Status,
                        @PerformedBy AS IssuedBy,
                        GETDATE() AS IssueDate
                    FROM BOMItems bi
                    WHERE bi.BOMID = @BOMID;
                END", connection);

            command.Parameters.AddWithValue("@PlanID", planId);
            command.Parameters.AddWithValue("@PerformedBy", performedBy > 0 ? performedBy : (object)DBNull.Value);

            await command.ExecuteNonQueryAsync();
            return true;
        }
    }
}

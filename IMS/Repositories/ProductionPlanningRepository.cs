using IMS.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public class ProductionPlanningRepository : IProductionPlanningRepository
    {
        private readonly string _connectionString;

        public ProductionPlanningRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<IEnumerable<ProductionPlanningDto>> GetAllProductionPlansAsync()
        {
            var results = new List<ProductionPlanningDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT pp.*, 
                           b.BOMVersion, 
                           p.ProductID, p.SKU, p.ProductName,
                           u.FullName AS SupervisorName,
                           mi.IssueCount AS MaterialIssueCount,
                           mi.PendingIssues AS PendingMaterialIssues,
                           fgr.ReceiptCount AS FinishedGoodsReceiptCount,
                           fgr.TotalAcceptedQty AS TotalAcceptedFinishedQty
                    FROM ProductionPlans pp
                    INNER JOIN BOM b ON pp.BOMID = b.BOMID
                    INNER JOIN Products p ON b.ProductID = p.ProductID
                    LEFT JOIN Users u ON pp.AssignedSupervisor = u.UserID
                    LEFT JOIN (
                        SELECT PlanID, 
                               COUNT(*) AS IssueCount,
                               SUM(CASE WHEN Status != 'Issued' THEN 1 ELSE 0 END) AS PendingIssues
                        FROM ProductionMaterialIssues
                        GROUP BY PlanID
                    ) mi ON pp.PlanID = mi.PlanID
                    LEFT JOIN (
                        SELECT PlanID,
                               COUNT(*) AS ReceiptCount,
                               SUM(AcceptedQty) AS TotalAcceptedQty
                        FROM FinishedGoodsReceipts
                        GROUP BY PlanID
                    ) fgr ON pp.PlanID = fgr.PlanID
                    ORDER BY CASE pp.Priority 
                                WHEN 'URGENT' THEN 1 
                                WHEN 'HIGH' THEN 2 
                                WHEN 'Normal' THEN 3 
                                ELSE 4 END, 
                             pp.DueDate ASC", connection);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new ProductionPlanningDto
                    {
                        PlanID = reader.GetInt32("PlanID"),
                        PlanNumber = reader.GetString("PlanNumber"),
                        BOMID = reader.GetInt32("BOMID"),
                        BOMVersion = reader.GetString("BOMVersion"),
                        ProductID = reader.GetInt32("ProductID"),
                        SKU = reader.GetString("SKU"),
                        ProductName = reader.GetString("ProductName"),
                        PlannedQty = reader.GetDecimal("PlannedQty"),
                        ProducedQty = reader.GetDecimal("ProducedQty"),
                        StartDate = reader.IsDBNull("StartDate") ? (DateTime?)null : reader.GetDateTime("StartDate"),
                        DueDate = reader.IsDBNull("DueDate") ? (DateTime?)null : reader.GetDateTime("DueDate"),
                        Priority = reader.GetString("Priority"),
                        Status = reader.GetString("Status"),
                        AssignedSupervisor = reader.IsDBNull("AssignedSupervisor") ? (int?)null : reader.GetInt32("AssignedSupervisor"),
                        SupervisorName = reader.IsDBNull("SupervisorName") ? "" : reader.GetString("SupervisorName"),
                        MaterialIssueCount = reader.IsDBNull("MaterialIssueCount") ? 0 : reader.GetInt32("MaterialIssueCount"),
                        PendingMaterialIssues = reader.IsDBNull("PendingMaterialIssues") ? 0 : reader.GetInt32("PendingMaterialIssues"),
                        FinishedGoodsReceiptCount = reader.IsDBNull("FinishedGoodsReceiptCount") ? 0 : reader.GetInt32("FinishedGoodsReceiptCount"),
                        TotalAcceptedFinishedQty = reader.IsDBNull("TotalAcceptedFinishedQty") ? 0 : reader.GetDecimal("TotalAcceptedFinishedQty")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching production plans.", ex);
            }

            return results;
        }

        public async Task<int> CreateProductionPlanAsync(ProductionPlanCreateDto plan)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    INSERT INTO ProductionPlans (PlanNumber, BOMID, PlannedQty, ProducedQty, StartDate, DueDate, Priority, AssignedSupervisor, Status)
                    VALUES (@PlanNumber, @BOMID, @PlannedQty, 0, @StartDate, @DueDate, @Priority, @AssignedSupervisor, 'Draft');
                    SELECT SCOPE_IDENTITY();", connection);

                command.Parameters.AddWithValue("@PlanNumber", plan.PlanNumber);
                command.Parameters.AddWithValue("@BOMID", plan.BOMID);
                command.Parameters.AddWithValue("@PlannedQty", plan.PlannedQty);
                command.Parameters.AddWithValue("@StartDate", plan.StartDate);
                command.Parameters.AddWithValue("@DueDate", plan.DueDate);
                command.Parameters.AddWithValue("@Priority", plan.Priority);
                command.Parameters.AddWithValue("@AssignedSupervisor", plan.AssignedSupervisor);

                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating production plan.", ex);
            }
        }

        public async Task<bool> UpdateProductionPlanAsync(ProductionPlanUpdateDto plan)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    UPDATE ProductionPlans 
                    SET PlanNumber = @PlanNumber, BOMID = @BOMID, PlannedQty = @PlannedQty, 
                        StartDate = @StartDate, DueDate = @DueDate, Priority = @Priority, 
                        AssignedSupervisor = @AssignedSupervisor
                    WHERE PlanID = @PlanID AND Status IN ('Draft', 'Material Check')", connection);

                command.Parameters.AddWithValue("@PlanID", plan.PlanID);
                command.Parameters.AddWithValue("@PlanNumber", plan.PlanNumber);
                command.Parameters.AddWithValue("@BOMID", plan.BOMID);
                command.Parameters.AddWithValue("@PlannedQty", plan.PlannedQty);
                command.Parameters.AddWithValue("@StartDate", plan.StartDate);
                command.Parameters.AddWithValue("@DueDate", plan.DueDate);
                command.Parameters.AddWithValue("@Priority", plan.Priority);
                command.Parameters.AddWithValue("@AssignedSupervisor", plan.AssignedSupervisor);

                await connection.OpenAsync();
                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error updating production plan.", ex);
            }
        }

        public async Task<bool> ApproveProductionPlanAsync(int planId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(@"
                    UPDATE ProductionPlans 
                    SET Status = 'Approved'
                    WHERE PlanID = @PlanID AND Status IN ('Draft', 'Material Check');

                    DECLARE @BOMID INT, @PlannedQty DECIMAL(18,2), @Supervisor INT;
                    SELECT @BOMID = BOMID, @PlannedQty = PlannedQty, @Supervisor = AssignedSupervisor
                    FROM ProductionPlans WHERE PlanID = @PlanID;

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
                            @Supervisor AS IssuedBy,
                            GETDATE() AS IssueDate
                        FROM BOMItems bi
                        WHERE bi.BOMID = @BOMID;
                    END", connection);

                command.Parameters.AddWithValue("@PlanID", planId);
                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error approving production plan.", ex);
            }
        }

        public async Task<bool> StartProductionPlanAsync(int planId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(@"
                    UPDATE ProductionPlans 
                    SET Status = 'In Production'
                    WHERE PlanID = @PlanID AND Status = 'Approved';

                    DECLARE @BOMID INT, @PlannedQty DECIMAL(18,2), @Supervisor INT;
                    SELECT @BOMID = BOMID, @PlannedQty = PlannedQty, @Supervisor = AssignedSupervisor
                    FROM ProductionPlans WHERE PlanID = @PlanID;

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
                            @Supervisor AS IssuedBy,
                            GETDATE() AS IssueDate
                        FROM BOMItems bi
                        WHERE bi.BOMID = @BOMID;
                    END", connection);

                command.Parameters.AddWithValue("@PlanID", planId);
                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error starting production plan.", ex);
            }
        }

        public async Task<bool> CancelProductionPlanAsync(int planId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    UPDATE ProductionPlans 
                    SET Status = 'Cancelled'
                    WHERE PlanID = @PlanID AND Status IN ('Draft', 'Material Check', 'Approved')", connection);

                command.Parameters.AddWithValue("@PlanID", planId);

                await connection.OpenAsync();
                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error cancelling production plan.", ex);
            }
        }

        public async Task<ProductionPlanningDto?> GetProductionPlanByIdAsync(int planId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand("SELECT * FROM ProductionPlans WHERE PlanID = @PlanID", connection);
                command.Parameters.AddWithValue("@PlanID", planId);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new ProductionPlanningDto
                    {
                        PlanID = reader.GetInt32("PlanID"),
                        PlanNumber = reader.GetString("PlanNumber"),
                        BOMID = reader.GetInt32("BOMID"),
                        PlannedQty = reader.GetDecimal("PlannedQty"),
                        StartDate = reader.IsDBNull("StartDate") ? (DateTime?)null : reader.GetDateTime("StartDate"),
                        DueDate = reader.IsDBNull("DueDate") ? (DateTime?)null : reader.GetDateTime("DueDate"),
                        Priority = reader.GetString("Priority"),
                        Status = reader.GetString("Status")
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching production plan details.", ex);
            }
        }

        public async Task<bool> IsPlanNumberExistsAsync(string planNumber)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT COUNT(*) FROM ProductionPlans WHERE PlanNumber = @PlanNumber", connection);
            command.Parameters.AddWithValue("@PlanNumber", planNumber);
            await connection.OpenAsync();
            var count = (int)(await command.ExecuteScalarAsync() ?? 0);
            return count > 0;
        }

        public async Task<bool> IsPlanNumberExistsForOtherPlanAsync(string planNumber, int planId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT COUNT(*) FROM ProductionPlans WHERE PlanNumber = @PlanNumber AND PlanID != @PlanID", connection);
            command.Parameters.AddWithValue("@PlanNumber", planNumber);
            command.Parameters.AddWithValue("@PlanID", planId);
            await connection.OpenAsync();
            var count = (int)(await command.ExecuteScalarAsync() ?? 0);
            return count > 0;
        }

        public async Task<IEnumerable<dynamic>> GetActiveBomsAsync()
        {
            var results = new List<dynamic>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT b.BOMID, b.BOMVersion, p.SKU, p.ProductName
                FROM BOM b
                INNER JOIN Products p ON b.ProductID = p.ProductID
                WHERE b.Status = 'Active'
                ORDER BY p.ProductName", connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new { 
                    ID = reader.GetInt32(0), 
                    Version = reader.GetString(1),
                    SKU = reader.GetString(2),
                    Name = reader.GetString(3)
                });
            }
            return results;
        }

        public async Task<IEnumerable<dynamic>> GetActiveSupervisorsAsync()
        {
            var results = new List<dynamic>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT UserID, FullName FROM Users WHERE IsActive = 1 ORDER BY FullName", connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new { ID = reader.GetInt32(0), Name = reader.GetString(1) });
            }
            return results;
        }

        public async Task<IEnumerable<dynamic>> GetMaterialShortagesAsync(int planId)
        {
            var results = new List<dynamic>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT 
                    p.SKU, p.ProductName,
                    req.TotalRequired,
                    avail.TotalAvailable
                FROM ProductionPlans pp
                CROSS APPLY (
                    SELECT 
                        bi.RawMaterialID,
                        SUM(bi.RequiredQty * pp.PlannedQty * (1 + ISNULL(bi.WastagePercent,0)/100)) AS TotalRequired
                    FROM BOMItems bi
                    WHERE bi.BOMID = pp.BOMID
                    GROUP BY bi.RawMaterialID
                ) req
                CROSS APPLY (
                    SELECT ISNULL(SUM(CurrentQty - ReservedQty - QCHoldQty - DamagedQty), 0) AS TotalAvailable
                    FROM InventoryStocks
                    WHERE ProductID = req.RawMaterialID
                ) avail
                INNER JOIN Products p ON req.RawMaterialID = p.ProductID
                WHERE pp.PlanID = @PlanID AND req.TotalRequired > avail.TotalAvailable", connection);
            
            command.Parameters.AddWithValue("@PlanID", planId);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new { 
                    SKU = reader.GetString(0), 
                    Name = reader.GetString(1),
                    Required = reader.GetDecimal(2),
                    Available = reader.GetDecimal(3)
                });
            }
            return results;
        }

        public async Task<BomInspectionResultDto?> GetBomInspectionAsync(int bomId, decimal plannedQty)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT 
                    b.BOMID, b.BOMVersion,
                    p.ProductID AS FinishedProductID, p.SKU AS FinishedSKU, p.ProductName AS FinishedName, 
                    ISNULL(p.StandardCost, 0) AS FinishedCost,
                    bi.RawMaterialID, rm.SKU AS RawSKU, rm.ProductName AS RawName, 
                    ISNULL(u.UnitCode, 'Units') AS UnitCode,
                    bi.RequiredQty, ISNULL(bi.WastagePercent, 0) AS WastagePercent,
                    ISNULL(rm.StandardCost, 0) AS RawStandardCost,
                    ISNULL(avail.AvailableStock, 0) AS AvailableStock
                FROM BOM b
                INNER JOIN Products p ON b.ProductID = p.ProductID
                INNER JOIN BOMItems bi ON b.BOMID = bi.BOMID
                INNER JOIN Products rm ON bi.RawMaterialID = rm.ProductID
                LEFT JOIN Units u ON rm.UnitID = u.UnitID
                CROSS APPLY (
                    SELECT ISNULL(SUM(CurrentQty - ReservedQty - QCHoldQty - DamagedQty), 0) AS AvailableStock
                    FROM InventoryStocks
                    WHERE ProductID = bi.RawMaterialID
                ) avail
                WHERE b.BOMID = @BOMID", connection);

            command.Parameters.AddWithValue("@BOMID", bomId);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            BomInspectionResultDto? result = null;
            decimal totalMaterialCost = 0;
            int shortageCount = 0;

            while (await reader.ReadAsync())
            {
                if (result == null)
                {
                    result = new BomInspectionResultDto
                    {
                        BOMID = reader.GetInt32("BOMID"),
                        BOMVersion = reader.GetString("BOMVersion"),
                        FinishedProductID = reader.GetInt32("FinishedProductID"),
                        FinishedProductSKU = reader.GetString("FinishedSKU"),
                        FinishedProductName = reader.GetString("FinishedName"),
                        FinishedStandardCost = reader.GetDecimal("FinishedCost"),
                        PlannedBatchQty = plannedQty
                    };
                }

                decimal reqPerUnit = reader.GetDecimal("RequiredQty");
                decimal wastage = reader.GetDecimal("WastagePercent");
                decimal totalRequired = plannedQty * reqPerUnit * (1 + (wastage / 100m));
                decimal availableStock = reader.GetDecimal("AvailableStock");
                decimal unitCost = reader.GetDecimal("RawStandardCost");
                decimal totalCost = totalRequired * unitCost;

                if (totalRequired > availableStock)
                {
                    shortageCount++;
                }

                totalMaterialCost += totalCost;

                result.Components.Add(new BomInspectionItemDto
                {
                    RawMaterialID = reader.GetInt32("RawMaterialID"),
                    SKU = reader.GetString("RawSKU"),
                    ProductName = reader.GetString("RawName"),
                    UnitCode = reader.GetString("UnitCode"),
                    RequiredPerUnit = reqPerUnit,
                    WastagePercent = wastage,
                    TotalRequiredQty = totalRequired,
                    AvailableStock = availableStock,
                    UnitCost = unitCost,
                    TotalCost = totalCost
                });
            }

            if (result != null)
            {
                result.TotalComponents = result.Components.Count;
                result.ShortageCount = shortageCount;
                result.EstimatedBatchMaterialCost = totalMaterialCost;
            }

            return result;
        }
    }
}

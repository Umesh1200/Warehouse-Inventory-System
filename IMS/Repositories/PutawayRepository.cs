using IMS.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public class PutawayRepository : IPutawayRepository
    {
        private readonly string _connectionString;

        public PutawayRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<IEnumerable<PutawayDto>> GetPutawayTasksAsync()
        {
            var results = new List<PutawayDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                // Combined query for existing PutawayTasks and items cleared from QC needing tasks
                using var command = new SqlCommand(@"
                    -- Part 1: Items where QC is completed and waiting for Putaway Task creation
                    SELECT 
                        NULL AS TaskID,
                        'PENDING-QC' AS TaskNo,
                        qc.QCID,
                        COALESCE(poi.ProductID, fgr.ProductID) AS ProductID,
                        p.SKU,
                        p.ProductName,
                        p.ProductType,
                        CASE WHEN qc.FGReceiptID IS NOT NULL THEN 'Production' ELSE 'Purchase' END AS SourceType,
                        COALESCE(s.SupplierName, 'In-House Production') AS SupplierName,
                        COALESCE(po.PONumber, pp.PlanNumber, '') AS PONumber,
                        COALESCE(ri.BatchNumber, fgr.BatchNumber, '') AS BatchNumber,
                        qc.AcceptedQty AS QtyToPutaway,
                        NULL AS SuggestedShelfID,
                        '' AS SuggestedShelfCode,
                        NULL AS ActualShelfID,
                        '' AS ActualShelfCode,
                        '' AS WarehouseName,
                        '' AS FloorName,
                        '' AS RoomName,
                        '' AS RackName,
                        NULL AS AssignedTo,
                        '' AS AssignedToName,
                        'Pending Task' AS Status,
                        NULL AS CompletedDate,
                        0 AS ShelfAvailableCapacity,
                        0 AS ShelfUtilizationPercentage
                    FROM QCChecks qc
                    LEFT JOIN ReceivingItems ri ON qc.ReceiptID = ri.ReceiptID
                    LEFT JOIN PurchaseOrderItems poi ON ri.POItemID = poi.POItemID
                    LEFT JOIN PurchaseOrders po ON poi.POID = po.POID
                    LEFT JOIN Suppliers s ON po.SupplierID = s.SupplierID
                    LEFT JOIN FinishedGoodsReceipts fgr ON qc.FGReceiptID = fgr.FGReceiptID
                    LEFT JOIN ProductionPlans pp ON fgr.PlanID = pp.PlanID
                    INNER JOIN Products p ON p.ProductID = COALESCE(poi.ProductID, fgr.ProductID)
                    WHERE qc.AcceptedQty > 0
                    AND qc.QCID NOT IN (SELECT QCID FROM PutawayTasks)

                    UNION ALL

                    -- Part 2: Actual Putaway Tasks
                    SELECT 
                        pt.TaskID,
                        pt.TaskNo,
                        pt.QCID,
                        pt.ProductID,
                        p.SKU,
                        p.ProductName,
                        p.ProductType,
                        CASE WHEN qc.FGReceiptID IS NOT NULL THEN 'Production' ELSE 'Purchase' END AS SourceType,
                        COALESCE(s.SupplierName, 'In-House Production') AS SupplierName,
                        COALESCE(po.PONumber, pp.PlanNumber, '') AS PONumber,
                        COALESCE(ri.BatchNumber, fgr.BatchNumber, '') AS BatchNumber,
                        pt.QtyToPutaway,
                        pt.SuggestedShelfID,
                        sh.ShelfCode AS SuggestedShelfCode,
                        pt.ActualShelfID,
                        ash.ShelfCode AS ActualShelfCode,
                        w.WarehouseName,
                        f.FloorName,
                        rm.RoomName,
                        r.RackName,
                        pt.AssignedTo,
                        u.FullName AS AssignedToName,
                        pt.Status,
                        pt.CompletedDate,
                        dbo.fn_GetShelfAvailableCapacity(COALESCE(pt.ActualShelfID, pt.SuggestedShelfID)) AS ShelfAvailableCapacity,
                        CASE 
                            WHEN COALESCE(ash.MaxQty, sh.MaxQty) > 0 THEN ((COALESCE(ash.MaxQty, sh.MaxQty) - dbo.fn_GetShelfAvailableCapacity(COALESCE(pt.ActualShelfID, pt.SuggestedShelfID))) / COALESCE(ash.MaxQty, sh.MaxQty)) * 100 
                            ELSE 0 
                        END AS ShelfUtilizationPercentage
                    FROM PutawayTasks pt
                    INNER JOIN Products p ON pt.ProductID = p.ProductID
                    INNER JOIN QCChecks qc ON pt.QCID = qc.QCID
                    LEFT JOIN ReceivingItems ri ON qc.ReceiptID = ri.ReceiptID
                    LEFT JOIN PurchaseOrderItems poi ON ri.POItemID = poi.POItemID
                    LEFT JOIN PurchaseOrders po ON poi.POID = po.POID
                    LEFT JOIN Suppliers s ON po.SupplierID = s.SupplierID
                    LEFT JOIN FinishedGoodsReceipts fgr ON qc.FGReceiptID = fgr.FGReceiptID
                    LEFT JOIN ProductionPlans pp ON fgr.PlanID = pp.PlanID
                    LEFT JOIN Shelves sh ON pt.SuggestedShelfID = sh.ShelfID
                    LEFT JOIN Racks r ON sh.RackID = r.RackID
                    LEFT JOIN Rooms rm ON r.RoomID = rm.RoomID
                    LEFT JOIN Floors f ON rm.FloorID = f.FloorID
                    LEFT JOIN Warehouses w ON f.WarehouseID = w.WarehouseID
                    LEFT JOIN Shelves ash ON pt.ActualShelfID = ash.ShelfID
                    LEFT JOIN Users u ON pt.AssignedTo = u.UserID

                    ORDER BY Status, TaskID DESC", connection);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new PutawayDto
                    {
                        TaskID = reader.IsDBNull("TaskID") ? (int?)null : reader.GetInt32("TaskID"),
                        TaskNo = reader.GetString("TaskNo"),
                        QCID = reader.GetInt32("QCID"),
                        ProductID = reader.GetInt32("ProductID"),
                        SKU = reader.GetString("SKU"),
                        ProductName = reader.GetString("ProductName"),
                        ProductType = reader.IsDBNull("ProductType") ? "" : reader.GetString("ProductType"),
                        SourceType = reader.IsDBNull("SourceType") ? "Purchase" : reader.GetString("SourceType"),
                        SupplierName = reader.GetString("SupplierName"),
                        PONumber = reader.GetString("PONumber"),
                        BatchNumber = reader.IsDBNull("BatchNumber") ? "" : reader.GetString("BatchNumber"),
                        QtyToPutaway = reader.GetDecimal("QtyToPutaway"),
                        SuggestedShelfID = reader.IsDBNull("SuggestedShelfID") ? (int?)null : reader.GetInt32("SuggestedShelfID"),
                        SuggestedShelfCode = reader.IsDBNull("SuggestedShelfCode") ? "" : reader.GetString("SuggestedShelfCode"),
                        ActualShelfID = reader.IsDBNull("ActualShelfID") ? (int?)null : reader.GetInt32("ActualShelfID"),
                        ActualShelfCode = reader.IsDBNull("ActualShelfCode") ? "" : reader.GetString("ActualShelfCode"),
                        WarehouseName = reader.IsDBNull("WarehouseName") ? "" : reader.GetString("WarehouseName"),
                        FloorName = reader.IsDBNull("FloorName") ? "" : reader.GetString("FloorName"),
                        RoomName = reader.IsDBNull("RoomName") ? "" : reader.GetString("RoomName"),
                        RackName = reader.IsDBNull("RackName") ? "" : reader.GetString("RackName"),
                        AssignedTo = reader.IsDBNull("AssignedTo") ? (int?)null : reader.GetInt32("AssignedTo"),
                        AssignedToName = reader.IsDBNull("AssignedToName") ? "" : reader.GetString("AssignedToName"),
                        Status = reader.GetString("Status"),
                        CompletedDate = reader.IsDBNull("CompletedDate") ? (DateTime?)null : reader.GetDateTime("CompletedDate"),
                        ShelfAvailableCapacity = reader.GetDecimal("ShelfAvailableCapacity"),
                        ShelfUtilizationPercentage = reader.GetDecimal("ShelfUtilizationPercentage")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching putaway tasks.", ex);
            }

            return results;
        }

        public async Task<PutawayActionResultDto> CreatePutawayTaskAsync(PutawayTaskCreateDto task)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                using var command = new SqlCommand(@"
                    INSERT INTO PutawayTasks (TaskNo, QCID, ProductID, QtyToPutaway, SuggestedShelfID, AssignedTo, Status)
                    VALUES ('PT-' + FORMAT(GETDATE(), 'yyyyMMdd-HHmmss'), @QCID, @ProductID, @QtyToPutaway, @SuggestedShelfID, @AssignedTo, 'Pending');
                    SELECT SCOPE_IDENTITY();", connection, transaction);

                command.Parameters.AddWithValue("@QCID", task.QCID);
                command.Parameters.AddWithValue("@ProductID", task.ProductID);
                command.Parameters.AddWithValue("@QtyToPutaway", task.QtyToPutaway);
                command.Parameters.AddWithValue("@SuggestedShelfID", task.SuggestedShelfID ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@AssignedTo", task.AssignedTo ?? (object)DBNull.Value);

                var taskId = Convert.ToInt32(await command.ExecuteScalarAsync());

                await transaction.CommitAsync();
                return new PutawayActionResultDto { Success = true, Message = "Putaway task created successfully.", NewID = taskId };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new PutawayActionResultDto { Success = false, Message = "Database error: " + ex.Message };
            }
        }

        public async Task<PutawayActionResultDto> CompletePutawayAsync(PutawayCompleteDto putaway)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using (var command = new SqlCommand("dbo.sp_ConfirmPutaway", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@TaskID", putaway.TaskID);
                    command.Parameters.AddWithValue("@ActualShelfID", putaway.ActualShelfID);
                    command.Parameters.AddWithValue("@PerformedBy", putaway.PerformedBy);
                    await command.ExecuteNonQueryAsync();
                }

                // If linked to FGReceipt, update FinishedGoodsReceipts to Putaway Completed
                using (var cmdUpdateFG = new SqlCommand(@"
                    UPDATE fgr
                    SET fgr.Status = 'Putaway Completed'
                    FROM FinishedGoodsReceipts fgr
                    INNER JOIN QCChecks qc ON fgr.FGReceiptID = qc.FGReceiptID
                    INNER JOIN PutawayTasks pt ON qc.QCID = pt.QCID
                    WHERE pt.TaskID = @TaskID", connection))
                {
                    cmdUpdateFG.Parameters.AddWithValue("@TaskID", putaway.TaskID);
                    await cmdUpdateFG.ExecuteNonQueryAsync();
                }

                return new PutawayActionResultDto { Success = true, Message = "Putaway completed. Stock has been added to inventory." };
            }
            catch (SqlException ex)
            {
                return new PutawayActionResultDto { Success = false, Message = ex.Message };
            }
            catch (Exception ex)
            {
                return new PutawayActionResultDto { Success = false, Message = "An unexpected error occurred during putaway completion. " + ex.Message };
            }
        }

        public async Task<QCItemForPutawayDto?> GetQCItemForPutawayAsync(int qcId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT qc.QCID,
                       COALESCE(poi.ProductID, fgr.ProductID) AS ProductID,
                       qc.AcceptedQty
                FROM QCChecks qc
                LEFT JOIN ReceivingItems ri ON qc.ReceiptID = ri.ReceiptID
                LEFT JOIN PurchaseOrderItems poi ON ri.POItemID = poi.POItemID
                LEFT JOIN FinishedGoodsReceipts fgr ON qc.FGReceiptID = fgr.FGReceiptID
                WHERE qc.QCID = @QCID", connection);

            command.Parameters.AddWithValue("@QCID", qcId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new QCItemForPutawayDto
                {
                    QCID = reader.GetInt32("QCID"),
                    ProductID = reader.GetInt32("ProductID"),
                    AcceptedQty = reader.GetDecimal("AcceptedQty")
                };
            }
            return null;
        }

        public async Task<PutawayDto?> GetPutawayTaskByIdAsync(int taskId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT * FROM PutawayTasks WHERE TaskID = @TaskID", connection);
            command.Parameters.AddWithValue("@TaskID", taskId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new PutawayDto
                {
                    TaskID = reader.GetInt32("TaskID"),
                    ProductID = reader.GetInt32("ProductID"),
                    Status = reader.GetString("Status"),
                    QtyToPutaway = reader.GetDecimal("QtyToPutaway")
                };
            }
            return null;
        }

        public async Task<bool> HasPutawayTaskForQCAsync(int qcId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT COUNT(*) FROM PutawayTasks WHERE QCID = @QCID", connection);
            command.Parameters.AddWithValue("@QCID", qcId);
            await connection.OpenAsync();
            var count = (int)(await command.ExecuteScalarAsync() ?? 0);
            return count > 0;
        }

        public async Task<IEnumerable<ShelfOptionDto>> GetActiveShelvesAsync()
        {
            var results = new List<ShelfOptionDto>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT s.ShelfID, w.WarehouseName + ' -> ' + f.FloorName + ' -> ' + rm.RoomName + ' -> ' + rk.RackName + ' -> ' + s.ShelfCode + ' [' + s.ShelfType + ']' AS FullPath
                FROM Shelves s
                INNER JOIN Racks rk ON s.RackID = rk.RackID
                INNER JOIN Rooms rm ON rk.RoomID = rm.RoomID
                INNER JOIN Floors f ON rm.FloorID = f.FloorID
                INNER JOIN Warehouses w ON f.WarehouseID = w.WarehouseID
                WHERE s.IsActive = 1
                  AND s.ShelfType <> 'QC Hold'
                ORDER BY w.WarehouseName, s.ShelfCode", connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new ShelfOptionDto { ID = reader.GetInt32(0), Name = reader.GetString(1) });
            }
            return results;
        }

        public async Task<IEnumerable<ShelfOptionDto>> GetSuggestedShelvesAsync(int productId, decimal quantity)
        {
            var results = new List<ShelfOptionDto>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                DECLARE @RequiredWeight DECIMAL(18,2);
                SELECT @RequiredWeight = ISNULL(WeightKg, 0) * @Quantity
                FROM Products
                WHERE ProductID = @ProductID;

                SELECT
                    s.ShelfID,
                    s.ShelfCode,
                    s.ShelfType,
                    s.MaxQty,
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

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var quantityAvailable = reader.GetDecimal("QuantityAvailable");
                var weightAvailable = reader.GetDecimal("WeightAvailableKg");
                var isCompatible = reader.GetInt32("IsCompatible") == 1;
                var requiredWeight = await GetRequiredWeightAsync(productId, quantity);
                var hasCapacity = quantityAvailable >= quantity && weightAvailable >= requiredWeight;
                var maxQty = reader.GetDecimal("MaxQty");
                var utilization = reader.GetDecimal("UtilizationPercentage");
                var projectedUtilization = maxQty > 0
                    ? Math.Min(100, ((maxQty - quantityAvailable + quantity) / maxQty) * 100)
                    : utilization;

                results.Add(new ShelfOptionDto
                {
                    ID = reader.GetInt32("ShelfID"),
                    Name = reader.GetString("FullPath"),
                    ShelfCode = reader.GetString("ShelfCode"),
                    ShelfType = reader.GetString("ShelfType"),
                    QuantityAvailable = quantityAvailable,
                    WeightAvailableKg = weightAvailable,
                    UtilizationPercentage = utilization,
                    ProjectedUtilizationPercentage = projectedUtilization,
                    IsCompatible = isCompatible,
                    IsRecommended = isCompatible && hasCapacity,
                    Warning = isCompatible
                        ? hasCapacity ? "" : "Capacity too low"
                        : "Shelf type not allowed"
                });
            }

            return results;
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

        public async Task<decimal> GetShelfAvailableWeightCapacityAsync(int shelfId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT dbo.fn_GetShelfAvailableWeightCapacity(@ShelfID)", connection);
            command.Parameters.AddWithValue("@ShelfID", shelfId);

            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return result == DBNull.Value || result == null ? 0m : Convert.ToDecimal(result);
        }
    }
}

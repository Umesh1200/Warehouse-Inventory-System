using IMS.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public class QCRepository : IQCRepository
    {
        private readonly string _connectionString;

        public QCRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<IEnumerable<QCCheckDto>> GetQCTasksAsync()
        {
            var results = new List<QCCheckDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    -- Part 1: Completed QC Checks (Purchase Orders)
                    SELECT 
                        qc.QCID,
                        ri.ReceiptID,
                        CAST(NULL AS INT) AS FGReceiptID,
                        'Purchase' AS SourceType,
                        po.PONumber,
                        s.SupplierName,
                        p.ProductID,
                        p.SKU,
                        p.ProductName,
                        ri.ReceivedQty,
                        qc.AcceptedQty,
                        qc.RejectedQty,
                        qc.DamagedQty,
                        qc.InspectorID,
                        u.FullName AS InspectorName,
                        qc.InspectionDate,
                        qc.Remarks,
                        qc.Status,
                        ri.Status AS ReceiptStatus,
                        ri.BatchNumber
                    FROM QCChecks qc
                    INNER JOIN ReceivingItems ri ON qc.ReceiptID = ri.ReceiptID
                    INNER JOIN PurchaseOrderItems poi ON ri.POItemID = poi.POItemID
                    INNER JOIN PurchaseOrders po ON poi.POID = po.POID
                    INNER JOIN Suppliers s ON po.SupplierID = s.SupplierID
                    INNER JOIN Products p ON poi.ProductID = p.ProductID
                    LEFT JOIN Users u ON qc.InspectorID = u.UserID
                    WHERE qc.ReceiptID IS NOT NULL

                    UNION ALL

                    -- Part 2: Completed QC Checks (Production Finished Goods)
                    SELECT 
                        qc.QCID,
                        CAST(NULL AS INT) AS ReceiptID,
                        fgr.FGReceiptID,
                        'Production' AS SourceType,
                        pp.PlanNumber AS PONumber,
                        'In-House Production' AS SupplierName,
                        p.ProductID,
                        p.SKU,
                        p.ProductName,
                        fgr.ProducedQty AS ReceivedQty,
                        qc.AcceptedQty,
                        qc.RejectedQty,
                        qc.DamagedQty,
                        qc.InspectorID,
                        u.FullName AS InspectorName,
                        qc.InspectionDate,
                        qc.Remarks,
                        qc.Status,
                        fgr.Status AS ReceiptStatus,
                        fgr.BatchNumber
                    FROM QCChecks qc
                    INNER JOIN FinishedGoodsReceipts fgr ON qc.FGReceiptID = fgr.FGReceiptID
                    INNER JOIN ProductionPlans pp ON fgr.PlanID = pp.PlanID
                    INNER JOIN Products p ON fgr.ProductID = p.ProductID
                    LEFT JOIN Users u ON qc.InspectorID = u.UserID
                    WHERE qc.FGReceiptID IS NOT NULL

                    UNION ALL

                    -- Part 3: Pending QC (Purchase Orders marked as QC Required)
                    SELECT 
                        NULL AS QCID,
                        ri.ReceiptID,
                        CAST(NULL AS INT) AS FGReceiptID,
                        'Purchase' AS SourceType,
                        po.PONumber,
                        s.SupplierName,
                        p.ProductID,
                        p.SKU,
                        p.ProductName,
                        ri.ReceivedQty,
                        0 AS AcceptedQty,
                        0 AS RejectedQty,
                        0 AS DamagedQty,
                        NULL AS InspectorID,
                        '' AS InspectorName,
                        NULL AS InspectionDate,
                        '' AS Remarks,
                        'Pending' AS Status,
                        ri.Status AS ReceiptStatus,
                        ri.BatchNumber
                    FROM ReceivingItems ri
                    INNER JOIN PurchaseOrderItems poi ON ri.POItemID = poi.POItemID
                    INNER JOIN PurchaseOrders po ON poi.POID = po.POID
                    INNER JOIN Suppliers s ON po.SupplierID = s.SupplierID
                    INNER JOIN Products p ON poi.ProductID = p.ProductID
                    WHERE ri.Status = 'QC Required'
                    AND ri.ReceiptID NOT IN (SELECT ReceiptID FROM QCChecks WHERE ReceiptID IS NOT NULL)

                    UNION ALL

                    -- Part 4: Pending QC (Production Finished Goods marked as QC Required / Pending QC)
                    SELECT 
                        NULL AS QCID,
                        CAST(NULL AS INT) AS ReceiptID,
                        fgr.FGReceiptID,
                        'Production' AS SourceType,
                        pp.PlanNumber AS PONumber,
                        'In-House Production' AS SupplierName,
                        p.ProductID,
                        p.SKU,
                        p.ProductName,
                        fgr.ProducedQty AS ReceivedQty,
                        0 AS AcceptedQty,
                        0 AS RejectedQty,
                        0 AS DamagedQty,
                        NULL AS InspectorID,
                        '' AS InspectorName,
                        NULL AS InspectionDate,
                        '' AS Remarks,
                        'Pending' AS Status,
                        fgr.Status AS ReceiptStatus,
                        fgr.BatchNumber
                    FROM FinishedGoodsReceipts fgr
                    INNER JOIN ProductionPlans pp ON fgr.PlanID = pp.PlanID
                    INNER JOIN Products p ON fgr.ProductID = p.ProductID
                    WHERE fgr.Status IN ('QC Required', 'Pending QC')
                    AND fgr.FGReceiptID NOT IN (SELECT FGReceiptID FROM QCChecks WHERE FGReceiptID IS NOT NULL)

                    ORDER BY InspectionDate DESC, QCID DESC", connection);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new QCCheckDto
                    {
                        QCID = reader.IsDBNull("QCID") ? (int?)null : reader.GetInt32("QCID"),
                        ReceiptID = reader.IsDBNull("ReceiptID") ? (int?)null : reader.GetInt32("ReceiptID"),
                        FGReceiptID = reader.IsDBNull("FGReceiptID") ? (int?)null : reader.GetInt32("FGReceiptID"),
                        SourceType = reader.GetString("SourceType"),
                        PONumber = reader.GetString("PONumber"),
                        SupplierName = reader.GetString("SupplierName"),
                        ProductID = reader.GetInt32("ProductID"),
                        SKU = reader.GetString("SKU"),
                        ProductName = reader.GetString("ProductName"),
                        ReceivedQty = reader.GetDecimal("ReceivedQty"),
                        AcceptedQty = reader.GetDecimal("AcceptedQty"),
                        RejectedQty = reader.GetDecimal("RejectedQty"),
                        DamagedQty = reader.GetDecimal("DamagedQty"),
                        InspectorID = reader.IsDBNull("InspectorID") ? (int?)null : reader.GetInt32("InspectorID"),
                        InspectorName = reader.IsDBNull("InspectorName") ? "" : reader.GetString("InspectorName"),
                        InspectionDate = reader.IsDBNull("InspectionDate") ? (DateTime?)null : reader.GetDateTime("InspectionDate"),
                        Remarks = reader.IsDBNull("Remarks") ? "" : reader.GetString("Remarks"),
                        Status = reader.GetString("Status"),
                        ReceiptStatus = reader.GetString("ReceiptStatus"),
                        BatchNumber = reader.IsDBNull("BatchNumber") ? "" : reader.GetString("BatchNumber")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching QC tasks.", ex);
            }

            return results;
        }

        public async Task<int> SubmitQCResultAsync(QCSubmitDto qc)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                string newStatus = (qc.AcceptedQty > 0) ? "Accepted" : "Rejected";

                if (qc.FGReceiptID.HasValue && qc.FGReceiptID > 0)
                {
                    // Finished Goods QC
                    using var cmdInsert = new SqlCommand(@"
                        INSERT INTO QCChecks (ReceiptID, FGReceiptID, AcceptedQty, RejectedQty, DamagedQty, Remarks, InspectorID, InspectionDate, Status)
                        VALUES (NULL, @FGReceiptID, @AcceptedQty, @RejectedQty, @DamagedQty, @Remarks, @InspectorID, GETDATE(), @Status);
                        SELECT SCOPE_IDENTITY();", connection, transaction);

                    cmdInsert.Parameters.AddWithValue("@FGReceiptID", qc.FGReceiptID.Value);
                    cmdInsert.Parameters.AddWithValue("@AcceptedQty", qc.AcceptedQty);
                    cmdInsert.Parameters.AddWithValue("@RejectedQty", qc.RejectedQty);
                    cmdInsert.Parameters.AddWithValue("@DamagedQty", qc.DamagedQty);
                    cmdInsert.Parameters.AddWithValue("@Remarks", qc.Remarks ?? (object)DBNull.Value);
                    cmdInsert.Parameters.AddWithValue("@InspectorID", qc.InspectorID);
                    cmdInsert.Parameters.AddWithValue("@Status", newStatus);

                    var qcId = Convert.ToInt32(await cmdInsert.ExecuteScalarAsync());

                    // Update FinishedGoodsReceipts
                    using var cmdUpdateFG = new SqlCommand(@"
                        UPDATE FinishedGoodsReceipts
                        SET Status = CASE WHEN @AcceptedQty > 0 THEN 'QC Completed' ELSE 'QC Rejected' END,
                            AcceptedQty = @AcceptedQty,
                            RejectedQty = @RejectedQty + @DamagedQty
                        WHERE FGReceiptID = @FGReceiptID", connection, transaction);

                    cmdUpdateFG.Parameters.AddWithValue("@AcceptedQty", qc.AcceptedQty);
                    cmdUpdateFG.Parameters.AddWithValue("@RejectedQty", qc.RejectedQty);
                    cmdUpdateFG.Parameters.AddWithValue("@DamagedQty", qc.DamagedQty);
                    cmdUpdateFG.Parameters.AddWithValue("@FGReceiptID", qc.FGReceiptID.Value);
                    await cmdUpdateFG.ExecuteNonQueryAsync();

                    await transaction.CommitAsync();
                    return qcId;
                }
                else
                {
                    // Purchase Order QC
                    using var cmdInsert = new SqlCommand(@"
                        INSERT INTO QCChecks (ReceiptID, FGReceiptID, AcceptedQty, RejectedQty, DamagedQty, Remarks, InspectorID, InspectionDate, Status)
                        VALUES (@ReceiptID, NULL, @AcceptedQty, @RejectedQty, @DamagedQty, @Remarks, @InspectorID, GETDATE(), @Status);
                        SELECT SCOPE_IDENTITY();", connection, transaction);

                    cmdInsert.Parameters.AddWithValue("@ReceiptID", qc.ReceiptID!.Value);
                    cmdInsert.Parameters.AddWithValue("@AcceptedQty", qc.AcceptedQty);
                    cmdInsert.Parameters.AddWithValue("@RejectedQty", qc.RejectedQty);
                    cmdInsert.Parameters.AddWithValue("@DamagedQty", qc.DamagedQty);
                    cmdInsert.Parameters.AddWithValue("@Remarks", qc.Remarks ?? (object)DBNull.Value);
                    cmdInsert.Parameters.AddWithValue("@InspectorID", qc.InspectorID);
                    cmdInsert.Parameters.AddWithValue("@Status", newStatus);

                    var qcId = Convert.ToInt32(await cmdInsert.ExecuteScalarAsync());

                    // Update ReceivingItems status and propagate damaged/rejected quantities
                    using var cmdUpdate = new SqlCommand(@"
                        UPDATE ReceivingItems
                        SET Status = @Status, 
                            DamagedQty = DamagedQty + @DamagedQty, 
                            RejectedQty = RejectedQty + @RejectedQty
                        WHERE ReceiptID = @ReceiptID", connection, transaction);

                    cmdUpdate.Parameters.AddWithValue("@Status", newStatus);
                    cmdUpdate.Parameters.AddWithValue("@DamagedQty", qc.DamagedQty);
                    cmdUpdate.Parameters.AddWithValue("@RejectedQty", qc.RejectedQty);
                    cmdUpdate.Parameters.AddWithValue("@ReceiptID", qc.ReceiptID.Value);
                    await cmdUpdate.ExecuteNonQueryAsync();

                    // If rejected or damaged quantities, move them to Quarantine shelf
                    decimal rejectedOrDamagedQty = qc.RejectedQty + qc.DamagedQty;
                    if (rejectedOrDamagedQty > 0)
                    {
                        int productID = 0;
                        using (var cmdGetProd = new SqlCommand(@"
                            SELECT poi.ProductID 
                            FROM ReceivingItems ri 
                            INNER JOIN PurchaseOrderItems poi ON ri.POItemID = poi.POItemID 
                            WHERE ri.ReceiptID = @ReceiptID", connection, transaction))
                        {
                            cmdGetProd.Parameters.AddWithValue("@ReceiptID", qc.ReceiptID.Value);
                            var prodResult = await cmdGetProd.ExecuteScalarAsync();
                            if (prodResult != null)
                            {
                                productID = Convert.ToInt32(prodResult);
                            }
                        }

                        if (productID > 0)
                        {
                            int quarantineShelfID = 0;
                            using (var cmdGetShelf = new SqlCommand(@"
                                SELECT TOP 1 ShelfID 
                                FROM Shelves 
                                WHERE ShelfType = 'Quarantine' AND IsActive = 1", connection, transaction))
                            {
                                var shelfResult = await cmdGetShelf.ExecuteScalarAsync();
                                if (shelfResult != null)
                                {
                                    quarantineShelfID = Convert.ToInt32(shelfResult);
                                }
                            }

                            if (quarantineShelfID > 0)
                            {
                                using (var cmdExist = new SqlCommand(@"
                                    IF NOT EXISTS (SELECT 1 FROM InventoryStocks WHERE ProductID = @ProductID AND ShelfID = @ShelfID)
                                    BEGIN
                                        INSERT INTO InventoryStocks (ProductID, ShelfID, CurrentQty, LastUpdated)
                                        VALUES (@ProductID, @ShelfID, 0, GETDATE())
                                    END", connection, transaction))
                                {
                                    cmdExist.Parameters.AddWithValue("@ProductID", productID);
                                    cmdExist.Parameters.AddWithValue("@ShelfID", quarantineShelfID);
                                    await cmdExist.ExecuteNonQueryAsync();
                                }

                                using (var cmdAddStock = new SqlCommand(@"
                                    UPDATE InventoryStocks
                                    SET CurrentQty = CurrentQty + @Qty,
                                        LastUpdated = GETDATE()
                                    WHERE ProductID = @ProductID AND ShelfID = @ShelfID", connection, transaction))
                                {
                                    cmdAddStock.Parameters.AddWithValue("@ProductID", productID);
                                    cmdAddStock.Parameters.AddWithValue("@ShelfID", quarantineShelfID);
                                    cmdAddStock.Parameters.AddWithValue("@Qty", rejectedOrDamagedQty);
                                    await cmdAddStock.ExecuteNonQueryAsync();
                                }

                                using (var cmdLogMove = new SqlCommand("dbo.sp_RecordStockMovement", connection, transaction))
                                {
                                    cmdLogMove.CommandType = CommandType.StoredProcedure;
                                    cmdLogMove.Parameters.AddWithValue("@ProductID", productID);
                                    cmdLogMove.Parameters.AddWithValue("@ShelfID", quarantineShelfID);
                                    cmdLogMove.Parameters.AddWithValue("@MovementType", "StockIn");
                                    cmdLogMove.Parameters.AddWithValue("@Quantity", rejectedOrDamagedQty);
                                    cmdLogMove.Parameters.AddWithValue("@ReferenceType", "QCCheck");
                                    cmdLogMove.Parameters.AddWithValue("@ReferenceNo", "QC-" + qcId);
                                    cmdLogMove.Parameters.AddWithValue("@PerformedBy", qc.InspectorID);
                                    cmdLogMove.Parameters.AddWithValue("@Remarks", "QC rejection/damage quarantine entry. Remarks: " + (qc.Remarks ?? "None"));
                                    await cmdLogMove.ExecuteNonQueryAsync();
                                }

                                int supplierID = 0;
                                decimal unitPrice = 0;
                                using (var cmdGetDetails = new SqlCommand(@"
                                    SELECT po.SupplierID, poi.UnitPrice 
                                    FROM ReceivingItems ri 
                                    INNER JOIN PurchaseOrderItems poi ON ri.POItemID = poi.POItemID 
                                    INNER JOIN PurchaseOrders po ON poi.POID = po.POID
                                    WHERE ri.ReceiptID = @ReceiptID", connection, transaction))
                                {
                                    cmdGetDetails.Parameters.AddWithValue("@ReceiptID", qc.ReceiptID.Value);
                                    using var readerDetails = await cmdGetDetails.ExecuteReaderAsync();
                                    if (await readerDetails.ReadAsync())
                                    {
                                        supplierID = readerDetails.GetInt32(0);
                                        unitPrice = readerDetails.GetDecimal(1);
                                    }
                                }

                                if (supplierID > 0)
                                {
                                    string dateStr = DateTime.Today.ToString("yyyyMMdd");
                                    int count = 1;
                                    using (var cmdCount = new SqlCommand(@"
                                        SELECT COUNT(*) 
                                        FROM SupplierReturns 
                                        WHERE CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE)", connection, transaction))
                                    {
                                        var countVal = await cmdCount.ExecuteScalarAsync();
                                        if (countVal != null)
                                        {
                                            count = Convert.ToInt32(countVal) + 1;
                                        }
                                    }
                                    string returnNo = $"RTV-{dateStr}-{count:D4}";

                                    int returnID = 0;
                                    using (var cmdInsertReturn = new SqlCommand(@"
                                        INSERT INTO SupplierReturns (ReturnNo, SupplierID, RMANumber, ReturnDate, Carrier, TrackingNumber, CreditStatus, Status, Remarks, CreatedBy, CreatedAt)
                                        VALUES (@ReturnNo, @SupplierID, NULL, GETDATE(), NULL, NULL, 'Pending', 'Draft', @Remarks, @InspectorID, GETDATE());
                                        SELECT SCOPE_IDENTITY();", connection, transaction))
                                    {
                                        cmdInsertReturn.Parameters.AddWithValue("@ReturnNo", returnNo);
                                        cmdInsertReturn.Parameters.AddWithValue("@SupplierID", supplierID);
                                        cmdInsertReturn.Parameters.AddWithValue("@Remarks", "Auto-generated from QC Check rejection for Receipt #" + qc.ReceiptID);
                                        cmdInsertReturn.Parameters.AddWithValue("@InspectorID", (object)qc.InspectorID ?? DBNull.Value);
                                        
                                        var returnIdVal = await cmdInsertReturn.ExecuteScalarAsync();
                                        if (returnIdVal != null)
                                        {
                                            returnID = Convert.ToInt32(returnIdVal);
                                        }
                                    }

                                    if (returnID > 0)
                                    {
                                        using (var cmdInsertItem = new SqlCommand(@"
                                            INSERT INTO SupplierReturnItems (ReturnID, ProductID, Quantity, UnitPrice, Reason, SourceShelfID)
                                            VALUES (@ReturnID, @ProductID, @Quantity, @UnitPrice, @Reason, @SourceShelfID)", connection, transaction))
                                        {
                                            cmdInsertItem.Parameters.AddWithValue("@ReturnID", returnID);
                                            cmdInsertItem.Parameters.AddWithValue("@ProductID", productID);
                                            cmdInsertItem.Parameters.AddWithValue("@Quantity", rejectedOrDamagedQty);
                                            cmdInsertItem.Parameters.AddWithValue("@UnitPrice", unitPrice);
                                            cmdInsertItem.Parameters.AddWithValue("@Reason", string.IsNullOrWhiteSpace(qc.Remarks) ? "QC Rejection" : qc.Remarks);
                                            cmdInsertItem.Parameters.AddWithValue("@SourceShelfID", quarantineShelfID);
                                            
                                            await cmdInsertItem.ExecuteNonQueryAsync();
                                        }
                                    }
                                }
                            }
                            else
                            {
                                throw new Exception("Unable to process QC rejection: No active Quarantine Shelf was found in the database. Please ensure a Quarantine Shelf exists and is active.");
                            }
                        }
                    }

                    await transaction.CommitAsync();
                    return qcId;
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception("Database error occurred while submitting QC result.", ex);
            }
        }

        public async Task<ReceivingItemForQCDto?> GetReceivingItemForQCAsync(int receiptId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT 
                    ri.ReceiptID, po.PONumber, s.SupplierName,
                    p.SKU, p.ProductName, ri.ReceivedQty,
                    ri.BatchNumber, ri.Status
                FROM ReceivingItems ri
                INNER JOIN PurchaseOrderItems poi ON ri.POItemID = poi.POItemID
                INNER JOIN PurchaseOrders po ON poi.POID = po.POID
                INNER JOIN Suppliers s ON po.SupplierID = s.SupplierID
                INNER JOIN Products p ON poi.ProductID = p.ProductID
                WHERE ri.ReceiptID = @ReceiptID", connection);

            command.Parameters.AddWithValue("@ReceiptID", receiptId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new ReceivingItemForQCDto
                {
                    ReceiptID = reader.GetInt32("ReceiptID"),
                    SourceType = "Purchase",
                    PONumber = reader.GetString("PONumber"),
                    SupplierName = reader.GetString("SupplierName"),
                    SKU = reader.GetString("SKU"),
                    ProductName = reader.GetString("ProductName"),
                    ReceivedQty = reader.GetDecimal("ReceivedQty"),
                    BatchNumber = reader.IsDBNull("BatchNumber") ? "" : reader.GetString("BatchNumber"),
                    Status = reader.GetString("Status")
                };
            }
            return null;
        }

        public async Task<ReceivingItemForQCDto?> GetFGReceiptForQCAsync(int fgReceiptId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT 
                    fgr.FGReceiptID, pp.PlanNumber, 'In-House Production' AS SupplierName,
                    p.SKU, p.ProductName, fgr.ProducedQty AS ReceivedQty,
                    fgr.BatchNumber, fgr.Status
                FROM FinishedGoodsReceipts fgr
                INNER JOIN ProductionPlans pp ON fgr.PlanID = pp.PlanID
                INNER JOIN Products p ON fgr.ProductID = p.ProductID
                WHERE fgr.FGReceiptID = @FGReceiptID", connection);

            command.Parameters.AddWithValue("@FGReceiptID", fgReceiptId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new ReceivingItemForQCDto
                {
                    FGReceiptID = reader.GetInt32("FGReceiptID"),
                    SourceType = "Production",
                    PONumber = reader.GetString("PlanNumber"),
                    SupplierName = "In-House Production",
                    SKU = reader.GetString("SKU"),
                    ProductName = reader.GetString("ProductName"),
                    ReceivedQty = reader.GetDecimal("ReceivedQty"),
                    BatchNumber = reader.IsDBNull("BatchNumber") ? "" : reader.GetString("BatchNumber"),
                    Status = reader.GetString("Status")
                };
            }
            return null;
        }

        public async Task<bool> HasQCCheckAsync(int receiptId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT COUNT(*) FROM QCChecks WHERE ReceiptID = @ReceiptID", connection);
            command.Parameters.AddWithValue("@ReceiptID", receiptId);
            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            var count = result != null ? (int)result : 0;
            return count > 0;
        }

        public async Task<bool> HasFGQCCheckAsync(int fgReceiptId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT COUNT(*) FROM QCChecks WHERE FGReceiptID = @FGReceiptID", connection);
            command.Parameters.AddWithValue("@FGReceiptID", fgReceiptId);
            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            var count = result != null ? (int)result : 0;
            return count > 0;
        }
    }
}

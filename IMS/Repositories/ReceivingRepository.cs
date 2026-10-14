using IMS.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public class ReceivingRepository : IReceivingRepository
    {
        private readonly string _connectionString;

        public ReceivingRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<IEnumerable<ReceivingDto>> GetReceivingTasksAsync()
        {
            var results = new List<ReceivingDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                // Combined query: 
                // 1. Items already received (history/current)
                // 2. Items confirmed/in transit but not yet fully received (expected)
                using var command = new SqlCommand(@"
                    -- Part 1: Items already received
                    SELECT 
                        ri.ReceiptID,
                        ri.POItemID,
                        po.PONumber,
                        s.SupplierName,
                        p.ProductID,
                        p.SKU,
                        p.ProductName,
                        poi.OrderedQty,
                        ri.ReceivedQty,
                        ri.DamagedQty,
                        ri.RejectedQty,
                        ri.BatchNumber,
                        ri.ReceivedBy,
                        u.FullName AS ReceivedByName,
                        ri.ReceiptDate,
                        po.ExpectedDate,
                        ri.Status
                    FROM ReceivingItems ri
                    INNER JOIN PurchaseOrderItems poi ON ri.POItemID = poi.POItemID
                    INNER JOIN PurchaseOrders po ON poi.POID = po.POID
                    INNER JOIN Suppliers s ON po.SupplierID = s.SupplierID
                    INNER JOIN Products p ON poi.ProductID = p.ProductID
                    LEFT JOIN Users u ON ri.ReceivedBy = u.UserID

                    UNION ALL

                    -- Part 2: PO items still waiting for full receipt
                    SELECT 
                        NULL AS ReceiptID,
                        poi.POItemID,
                        po.PONumber,
                        s.SupplierName,
                        p.ProductID,
                        p.SKU,
                        p.ProductName,
                        poi.OrderedQty,
                        poi.ReceivedQty,
                        0 AS DamagedQty,
                        0 AS RejectedQty,
                        '' AS BatchNumber,
                        NULL AS ReceivedBy,
                        '' AS ReceivedByName,
                        NULL AS ReceiptDate,
                        po.ExpectedDate,
                        CASE WHEN poi.ReceivedQty > 0 THEN 'Partial' ELSE 'Expected' END AS Status
                    FROM PurchaseOrderItems poi
                    INNER JOIN PurchaseOrders po ON poi.POID = po.POID
                    INNER JOIN Suppliers s ON po.SupplierID = s.SupplierID
                    INNER JOIN Products p ON poi.ProductID = p.ProductID
                    WHERE po.Status IN ('Confirmed', 'InTransit', 'Partial', 'Received')
                    AND poi.ReceivedQty < poi.OrderedQty

                    ORDER BY ExpectedDate ASC, ReceiptDate DESC, PONumber ASC", connection);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new ReceivingDto
                    {
                        ReceiptID = reader.IsDBNull("ReceiptID") ? (int?)null : reader.GetInt32("ReceiptID"),
                        POItemID = reader.GetInt32("POItemID"),
                        PONumber = reader.GetString("PONumber"),
                        SupplierName = reader.GetString("SupplierName"),
                        ProductID = reader.GetInt32("ProductID"),
                        SKU = reader.GetString("SKU"),
                        ProductName = reader.GetString("ProductName"),
                        OrderedQty = reader.GetDecimal("OrderedQty"),
                        ReceivedQty = reader.GetDecimal("ReceivedQty"),
                        DamagedQty = reader.GetDecimal("DamagedQty"),
                        RejectedQty = reader.GetDecimal("RejectedQty"),
                        BatchNumber = reader.IsDBNull("BatchNumber") ? "" : reader.GetString("BatchNumber"),
                        ReceivedBy = reader.IsDBNull("ReceivedBy") ? (int?)null : reader.GetInt32("ReceivedBy"),
                        ReceivedByName = reader.IsDBNull("ReceivedByName") ? "" : reader.GetString("ReceivedByName"),
                        ReceiptDate = reader.IsDBNull("ReceiptDate") ? (DateTime?)null : reader.GetDateTime("ReceiptDate"),
                        ExpectedDate = reader.IsDBNull("ExpectedDate") ? (DateTime?)null : reader.GetDateTime("ExpectedDate"),
                        Status = reader.GetString("Status")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching receiving tasks.", ex);
            }

            return results;
        }

        public async Task<int> CreateReceiptAsync(ReceivingCreateDto receipt)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Insert into ReceivingItems
                using var cmdInsert = new SqlCommand(@"
                    INSERT INTO ReceivingItems (POItemID, ReceivedQty, DamagedQty, RejectedQty, BatchNumber, ReceivedBy, ReceiptDate, Status)
                    VALUES (@POItemID, @ReceivedQty, @DamagedQty, @RejectedQty, @BatchNumber, @ReceivedBy, GETDATE(), 'QC Required');
                    SELECT SCOPE_IDENTITY();", connection, transaction);

                cmdInsert.Parameters.AddWithValue("@POItemID", receipt.POItemID);
                cmdInsert.Parameters.AddWithValue("@ReceivedQty", receipt.ReceivedQty);
                cmdInsert.Parameters.AddWithValue("@DamagedQty", receipt.DamagedQty);
                cmdInsert.Parameters.AddWithValue("@RejectedQty", receipt.RejectedQty);
                cmdInsert.Parameters.AddWithValue("@BatchNumber", receipt.BatchNumber ?? (object)DBNull.Value);
                cmdInsert.Parameters.AddWithValue("@ReceivedBy", receipt.ReceivedBy);

                var receiptId = Convert.ToInt32(await cmdInsert.ExecuteScalarAsync());

                // 2. Update PurchaseOrderItems
                using var cmdUpdateItem = new SqlCommand(@"
                    UPDATE PurchaseOrderItems 
                    SET ReceivedQty = ReceivedQty + @ReceivedQty 
                    WHERE POItemID = @POItemID", connection, transaction);

                cmdUpdateItem.Parameters.AddWithValue("@ReceivedQty", receipt.ReceivedQty);
                cmdUpdateItem.Parameters.AddWithValue("@POItemID", receipt.POItemID);
                await cmdUpdateItem.ExecuteNonQueryAsync();

                // 3. Update PurchaseOrders status based on all PO item quantities.
                using var cmdUpdatePO = new SqlCommand(@"
                    DECLARE @POID INT;
                    SELECT @POID = POID FROM PurchaseOrderItems WHERE POItemID = @POItemID;

                    UPDATE PurchaseOrders
                    SET Status = CASE
                        WHEN NOT EXISTS (
                            SELECT 1
                            FROM PurchaseOrderItems
                            WHERE POID = @POID
                              AND ReceivedQty < OrderedQty
                        ) THEN 'Received'
                        ELSE 'Partial'
                    END
                    WHERE POID = @POID
                      AND Status IN ('Confirmed', 'InTransit', 'Partial', 'Received')", connection, transaction);

                cmdUpdatePO.Parameters.AddWithValue("@POItemID", receipt.POItemID);
                await cmdUpdatePO.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
                return receiptId;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception("Database error occurred while recording receipt.", ex);
            }
        }

        public async Task<PurchaseOrderItemReceivingDto?> GetPOItemForReceivingAsync(int poItemId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT 
                    poi.POItemID, poi.POID, po.PONumber, s.SupplierName,
                    p.ProductID, p.SKU, p.ProductName,
                    poi.OrderedQty, poi.ReceivedQty AS PreviouslyReceivedQty
                FROM PurchaseOrderItems poi
                INNER JOIN PurchaseOrders po ON poi.POID = po.POID
                INNER JOIN Suppliers s ON po.SupplierID = s.SupplierID
                INNER JOIN Products p ON poi.ProductID = p.ProductID
                WHERE poi.POItemID = @POItemID", connection);

            command.Parameters.AddWithValue("@POItemID", poItemId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new PurchaseOrderItemReceivingDto
                {
                    POItemID = reader.GetInt32("POItemID"),
                    POID = reader.GetInt32("POID"),
                    PONumber = reader.GetString("PONumber"),
                    SupplierName = reader.GetString("SupplierName"),
                    ProductID = reader.GetInt32("ProductID"),
                    SKU = reader.GetString("SKU"),
                    ProductName = reader.GetString("ProductName"),
                    OrderedQty = reader.GetDecimal("OrderedQty"),
                    PreviouslyReceivedQty = reader.GetDecimal("PreviouslyReceivedQty")
                };
            }
            return null;
        }

        public async Task<decimal> GetRemainingQtyAsync(int poItemId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT OrderedQty - ReceivedQty 
                FROM PurchaseOrderItems 
                WHERE POItemID = @POItemID", connection);

            command.Parameters.AddWithValue("@POItemID", poItemId);
            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return result == DBNull.Value || result == null ? 0m : Convert.ToDecimal(result);
        }
    }
}

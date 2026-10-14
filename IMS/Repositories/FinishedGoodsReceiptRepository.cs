using IMS.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public class FinishedGoodsReceiptRepository : IFinishedGoodsReceiptRepository
    {
        private readonly string _connectionString;

        public FinishedGoodsReceiptRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<IEnumerable<FinishedGoodsReceiptDto>> GetAllReceiptsAsync()
        {
            var results = new List<FinishedGoodsReceiptDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT fgr.*, 
                           pp.PlanNumber, pp.Status AS ProductionStatus,
                           b.BOMVersion,
                           p.SKU, p.ProductName,
                           u.FullName AS ReceivedByName
                    FROM FinishedGoodsReceipts fgr
                    INNER JOIN ProductionPlans pp ON fgr.PlanID = pp.PlanID
                    INNER JOIN BOM b ON pp.BOMID = b.BOMID
                    INNER JOIN Products p ON fgr.ProductID = p.ProductID
                    LEFT JOIN Users u ON fgr.ReceivedBy = u.UserID
                    ORDER BY fgr.ReceiptDate DESC", connection);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new FinishedGoodsReceiptDto
                    {
                        FGReceiptID = reader.GetInt32("FGReceiptID"),
                        ReceiptNumber = reader.GetString("ReceiptNumber"),
                        PlanID = reader.GetInt32("PlanID"),
                        PlanNumber = reader.GetString("PlanNumber"),
                        ProductID = reader.GetInt32("ProductID"),
                        SKU = reader.GetString("SKU"),
                        ProductName = reader.GetString("ProductName"),
                        ProducedQty = reader.GetDecimal("ProducedQty"),
                        AcceptedQty = reader.GetDecimal("AcceptedQty"),
                        RejectedQty = reader.GetDecimal("RejectedQty"),
                        BatchNumber = reader.IsDBNull("BatchNumber") ? "" : reader.GetString("BatchNumber"),
                        ReceivedBy = reader.IsDBNull("ReceivedBy") ? (int?)null : reader.GetInt32("ReceivedBy"),
                        ReceivedByName = reader.IsDBNull("ReceivedByName") ? "" : reader.GetString("ReceivedByName"),
                        ReceiptDate = reader.GetDateTime("ReceiptDate"),
                        Status = reader.GetString("Status"),
                        BOMVersion = reader.GetString("BOMVersion"),
                        ProductionStatus = reader.GetString("ProductionStatus")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching finished goods receipts.", ex);
            }

            return results;
        }

        public async Task<int> CreateFinishedGoodsReceiptAsync(FinishedGoodsReceiptCreateDto receipt)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Insert Receipt
                using var cmdInsert = new SqlCommand(@"
                    INSERT INTO FinishedGoodsReceipts (ReceiptNumber, PlanID, ProductID, ProducedQty, AcceptedQty, RejectedQty, BatchNumber, ReceivedBy, ReceiptDate, Status)
                    VALUES (@ReceiptNumber, @PlanID, @ProductID, @ProducedQty, @AcceptedQty, @RejectedQty, @BatchNumber, @ReceivedBy, GETDATE(), 'Pending QC');
                    SELECT SCOPE_IDENTITY();", connection, transaction);

                cmdInsert.Parameters.AddWithValue("@ReceiptNumber", receipt.ReceiptNumber);
                cmdInsert.Parameters.AddWithValue("@PlanID", receipt.PlanID);
                cmdInsert.Parameters.AddWithValue("@ProductID", receipt.ProductID);
                cmdInsert.Parameters.AddWithValue("@ProducedQty", receipt.ProducedQty);
                cmdInsert.Parameters.AddWithValue("@AcceptedQty", receipt.AcceptedQty);
                cmdInsert.Parameters.AddWithValue("@RejectedQty", receipt.RejectedQty);
                cmdInsert.Parameters.AddWithValue("@BatchNumber", receipt.BatchNumber ?? (object)DBNull.Value);
                cmdInsert.Parameters.AddWithValue("@ReceivedBy", receipt.ReceivedBy);

                var receiptId = Convert.ToInt32(await cmdInsert.ExecuteScalarAsync());

                // 2. Update Production Plan progress
                using var cmdUpdatePlan = new SqlCommand(@"
                    UPDATE ProductionPlans 
                    SET ProducedQty = ProducedQty + @ProducedQty,
                        Status = CASE WHEN (ProducedQty + @ProducedQty) >= PlannedQty THEN 'Completed' ELSE Status END
                    WHERE PlanID = @PlanID", connection, transaction);

                cmdUpdatePlan.Parameters.AddWithValue("@ProducedQty", receipt.ProducedQty);
                cmdUpdatePlan.Parameters.AddWithValue("@PlanID", receipt.PlanID);
                await cmdUpdatePlan.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
                return receiptId;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception("Error recording finished goods receipt.", ex);
            }
        }

        public async Task<bool> ReceiveFinishedGoodsAsync(int receiptId, int performedBy)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand("dbo.sp_ReceiveFinishedGoods", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@FGReceiptID", receiptId);
                command.Parameters.AddWithValue("@PerformedBy", performedBy);

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error executing confirmation procedure.", ex);
            }
        }

        public async Task<ProductionPlanForReceiptDto?> GetProductionPlanForReceiptAsync(int planId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT pp.PlanID, pp.PlanNumber, p.ProductID, p.SKU, p.ProductName, pp.PlannedQty, pp.ProducedQty
                FROM ProductionPlans pp
                INNER JOIN BOM b ON pp.BOMID = b.BOMID
                INNER JOIN Products p ON b.ProductID = p.ProductID
                WHERE pp.PlanID = @PlanID", connection);

            command.Parameters.AddWithValue("@PlanID", planId);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new ProductionPlanForReceiptDto
                {
                    PlanID = reader.GetInt32("PlanID"),
                    PlanNumber = reader.GetString("PlanNumber"),
                    ProductID = reader.GetInt32("ProductID"),
                    SKU = reader.GetString("SKU"),
                    ProductName = reader.GetString("ProductName"),
                    PlannedQty = reader.GetDecimal("PlannedQty"),
                    ProducedQtySoFar = reader.GetDecimal("ProducedQty")
                };
            }
            return null;
        }

        public async Task<bool> IsReceiptNumberExistsAsync(string receiptNumber)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT COUNT(*) FROM FinishedGoodsReceipts WHERE ReceiptNumber = @ReceiptNumber", connection);
            command.Parameters.AddWithValue("@ReceiptNumber", receiptNumber);
            await connection.OpenAsync();
            var count = (int)(await command.ExecuteScalarAsync() ?? 0);
            return count > 0;
        }

        public async Task<IEnumerable<dynamic>> GetActiveProductionPlansAsync()
        {
            var results = new List<dynamic>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT pp.PlanID, pp.PlanNumber, p.SKU, p.ProductName
                FROM ProductionPlans pp
                INNER JOIN BOM b ON pp.BOMID = b.BOMID
                INNER JOIN Products p ON b.ProductID = p.ProductID
                WHERE pp.Status IN ('In Production', 'Approved')
                ORDER BY pp.PlanNumber", connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new { ID = reader.GetInt32(0), Number = reader.GetString(1), Product = $"[{reader.GetString(2)}] {reader.GetString(3)}" });
            }
            return results;
        }

        public async Task<IEnumerable<dynamic>> GetActiveStaffAsync()
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
    }
}

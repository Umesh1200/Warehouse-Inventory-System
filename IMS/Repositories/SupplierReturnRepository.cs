using IMS.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public class SupplierReturnRepository : ISupplierReturnRepository
    {
        private readonly string _connectionString;

        public SupplierReturnRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<IEnumerable<SupplierReturnDto>> GetAllReturnsAsync()
        {
            var list = new List<SupplierReturnDto>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT r.*, s.SupplierName, u.FullName AS CreatorName
                FROM SupplierReturns r
                INNER JOIN Suppliers s ON r.SupplierID = s.SupplierID
                LEFT JOIN Users u ON r.CreatedBy = u.UserID
                ORDER BY r.CreatedAt DESC", connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new SupplierReturnDto
                {
                    ReturnID = reader.GetInt32("ReturnID"),
                    ReturnNo = reader.GetString("ReturnNo"),
                    SupplierID = reader.GetInt32("SupplierID"),
                    SupplierName = reader.GetString("SupplierName"),
                    RMANumber = reader.IsDBNull("RMANumber") ? "" : reader.GetString("RMANumber"),
                    ReturnDate = reader.GetDateTime("ReturnDate"),
                    Carrier = reader.IsDBNull("Carrier") ? "" : reader.GetString("Carrier"),
                    TrackingNumber = reader.IsDBNull("TrackingNumber") ? "" : reader.GetString("TrackingNumber"),
                    CreditStatus = reader.GetString("CreditStatus"),
                    Status = reader.GetString("Status"),
                    Remarks = reader.IsDBNull("Remarks") ? "" : reader.GetString("Remarks"),
                    CreditNoteNo = reader.IsDBNull("CreditNoteNo") ? "" : reader.GetString("CreditNoteNo"),
                    CreditAmount = reader.IsDBNull("CreditAmount") ? (decimal?)null : reader.GetDecimal("CreditAmount"),
                    CreatedBy = reader.IsDBNull("CreatedBy") ? null : reader.GetInt32("CreatedBy"),
                    CreatorName = reader.IsDBNull("CreatorName") ? "" : reader.GetString("CreatorName"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    RejectionReason = reader.IsDBNull("RejectionReason") ? "" : reader.GetString("RejectionReason"),
                    ReplacementSKU = reader.IsDBNull("ReplacementSKU") ? "" : reader.GetString("ReplacementSKU"),
                    ReplacementQty = reader.IsDBNull("ReplacementQty") ? (decimal?)null : reader.GetDecimal("ReplacementQty"),
                    ReplacementTracking = reader.IsDBNull("ReplacementTracking") ? "" : reader.GetString("ReplacementTracking")
                });
            }
            return list;
        }

        public async Task<SupplierReturnDto?> GetReturnByIdAsync(int returnId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT r.*, s.SupplierName, u.FullName AS CreatorName
                FROM SupplierReturns r
                INNER JOIN Suppliers s ON r.SupplierID = s.SupplierID
                LEFT JOIN Users u ON r.CreatedBy = u.UserID
                WHERE r.ReturnID = @ReturnID", connection);
            command.Parameters.AddWithValue("@ReturnID", returnId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new SupplierReturnDto
                {
                    ReturnID = reader.GetInt32("ReturnID"),
                    ReturnNo = reader.GetString("ReturnNo"),
                    SupplierID = reader.GetInt32("SupplierID"),
                    SupplierName = reader.GetString("SupplierName"),
                    RMANumber = reader.IsDBNull("RMANumber") ? "" : reader.GetString("RMANumber"),
                    ReturnDate = reader.GetDateTime("ReturnDate"),
                    Carrier = reader.IsDBNull("Carrier") ? "" : reader.GetString("Carrier"),
                    TrackingNumber = reader.IsDBNull("TrackingNumber") ? "" : reader.GetString("TrackingNumber"),
                    CreditStatus = reader.GetString("CreditStatus"),
                    Status = reader.GetString("Status"),
                    Remarks = reader.IsDBNull("Remarks") ? "" : reader.GetString("Remarks"),
                    CreditNoteNo = reader.IsDBNull("CreditNoteNo") ? "" : reader.GetString("CreditNoteNo"),
                    CreditAmount = reader.IsDBNull("CreditAmount") ? (decimal?)null : reader.GetDecimal("CreditAmount"),
                    CreatedBy = reader.IsDBNull("CreatedBy") ? null : reader.GetInt32("CreatedBy"),
                    CreatorName = reader.IsDBNull("CreatorName") ? "" : reader.GetString("CreatorName"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    RejectionReason = reader.IsDBNull("RejectionReason") ? "" : reader.GetString("RejectionReason"),
                    ReplacementSKU = reader.IsDBNull("ReplacementSKU") ? "" : reader.GetString("ReplacementSKU"),
                    ReplacementQty = reader.IsDBNull("ReplacementQty") ? (decimal?)null : reader.GetDecimal("ReplacementQty"),
                    ReplacementTracking = reader.IsDBNull("ReplacementTracking") ? "" : reader.GetString("ReplacementTracking")
                };
            }
            return null;
        }

        public async Task<IEnumerable<SupplierReturnItemDto>> GetReturnItemsAsync(int returnId)
        {
            var list = new List<SupplierReturnItemDto>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT ri.*, p.SKU, p.ProductName, s.ShelfCode
                FROM SupplierReturnItems ri
                INNER JOIN Products p ON ri.ProductID = p.ProductID
                LEFT JOIN Shelves s ON ri.SourceShelfID = s.ShelfID
                WHERE ri.ReturnID = @ReturnID", connection);
            command.Parameters.AddWithValue("@ReturnID", returnId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new SupplierReturnItemDto
                {
                    ReturnItemID = reader.GetInt32("ReturnItemID"),
                    ReturnID = reader.GetInt32("ReturnID"),
                    ProductID = reader.GetInt32("ProductID"),
                    SKU = reader.GetString("SKU"),
                    ProductName = reader.GetString("ProductName"),
                    Quantity = reader.GetDecimal("Quantity"),
                    UnitPrice = reader.GetDecimal("UnitPrice"),
                    Reason = reader.IsDBNull("Reason") ? "" : reader.GetString("Reason"),
                    SourceShelfID = reader.IsDBNull("SourceShelfID") ? null : reader.GetInt32("SourceShelfID"),
                    SourceShelfCode = reader.IsDBNull("ShelfCode") ? "" : reader.GetString("ShelfCode")
                });
            }
            return list;
        }

        public async Task<int> CreateReturnAsync(SupplierReturnCreateDto returnDto, int createdBy)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Generate ReturnNo (e.g. RTV-yyyyMMdd-XXXX)
                string dateStr = DateTime.Today.ToString("yyyyMMdd");
                using var countCmd = new SqlCommand("SELECT COUNT(*) FROM SupplierReturns WHERE CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE)", connection, transaction);
                int count = Convert.ToInt32(await countCmd.ExecuteScalarAsync()) + 1;
                string returnNo = $"RTV-{dateStr}-{count:D4}";

                int supplierId = Convert.ToInt32(returnDto.SupplierID);

                // Insert Return Header
                using var insertHeaderCmd = new SqlCommand(@"
                    INSERT INTO SupplierReturns (ReturnNo, SupplierID, RMANumber, Carrier, TrackingNumber, Remarks, CreatedBy, Status, CreditStatus, CreatedAt)
                    VALUES (@ReturnNo, @SupplierID, @RMANumber, @Carrier, @TrackingNumber, @Remarks, @CreatedBy, 'Draft', 'Pending', GETDATE());
                    SELECT SCOPE_IDENTITY();", connection, transaction);

                insertHeaderCmd.Parameters.AddWithValue("@ReturnNo", returnNo);
                insertHeaderCmd.Parameters.AddWithValue("@SupplierID", supplierId);
                insertHeaderCmd.Parameters.AddWithValue("@RMANumber", returnDto.RMANumber ?? (object)DBNull.Value);
                insertHeaderCmd.Parameters.AddWithValue("@Carrier", returnDto.Carrier ?? (object)DBNull.Value);
                insertHeaderCmd.Parameters.AddWithValue("@TrackingNumber", returnDto.TrackingNumber ?? (object)DBNull.Value);
                insertHeaderCmd.Parameters.AddWithValue("@Remarks", returnDto.Remarks ?? (object)DBNull.Value);
                insertHeaderCmd.Parameters.AddWithValue("@CreatedBy", createdBy);

                int returnId = Convert.ToInt32(await insertHeaderCmd.ExecuteScalarAsync());

                // Insert Return Items
                foreach (var item in returnDto.Items)
                {
                    using var insertItemCmd = new SqlCommand(@"
                        INSERT INTO SupplierReturnItems (ReturnID, ProductID, Quantity, UnitPrice, Reason, SourceShelfID)
                        VALUES (@ReturnID, @ProductID, @Quantity, @UnitPrice, @Reason, @SourceShelfID);", connection, transaction);

                    insertItemCmd.Parameters.AddWithValue("@ReturnID", returnId);
                    insertItemCmd.Parameters.AddWithValue("@ProductID", item.ProductID);
                    insertItemCmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                    insertItemCmd.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);
                    insertItemCmd.Parameters.AddWithValue("@Reason", item.Reason ?? (object)DBNull.Value);
                    insertItemCmd.Parameters.AddWithValue("@SourceShelfID", item.SourceShelfID);

                    await insertItemCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return returnId;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> ConfirmReturnAsync(int returnId, int performedBy)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("dbo.sp_ConfirmSupplierReturn", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ReturnID", returnId);
            command.Parameters.AddWithValue("@PerformedBy", performedBy);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
            return true;
        }

        public async Task<bool> UpdateCreditStatusAsync(int returnId, string creditStatus)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("UPDATE SupplierReturns SET CreditStatus = @CreditStatus, Status = CASE WHEN @CreditStatus = 'Issued' THEN 'Completed' ELSE Status END, UpdatedAt = GETDATE() WHERE ReturnID = @ReturnID", connection);
            command.Parameters.AddWithValue("@ReturnID", returnId);
            command.Parameters.AddWithValue("@CreditStatus", creditStatus);

            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> CancelReturnAsync(int returnId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("UPDATE SupplierReturns SET Status = 'Cancelled', UpdatedAt = GETDATE() WHERE ReturnID = @ReturnID AND Status = 'Draft'", connection);
            command.Parameters.AddWithValue("@ReturnID", returnId);

            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<SupplierReturnSummaryDto> GetReturnsSummaryAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT 
                    COUNT(*) AS TotalReturns,
                    SUM(CASE WHEN CreditStatus = 'Pending' AND Status = 'Shipped' THEN 1 ELSE 0 END) AS PendingCredit,
                    SUM(CASE WHEN Status = 'Shipped' THEN 1 ELSE 0 END) AS ShippedReturns,
                    ISNULL(SUM(items.TotalVal), 0) AS TotalValue
                FROM SupplierReturns r
                OUTER APPLY (
                    SELECT SUM(Quantity * UnitPrice) AS TotalVal
                    FROM SupplierReturnItems
                    WHERE ReturnID = r.ReturnID
                ) items", connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new SupplierReturnSummaryDto
                {
                    TotalReturns = reader.GetInt32("TotalReturns"),
                    PendingCredit = reader.GetInt32("PendingCredit"),
                    ShippedReturns = reader.GetInt32("ShippedReturns"),
                    TotalValue = reader.GetDecimal("TotalValue")
                };
            }
            return new SupplierReturnSummaryDto();
        }

        public async Task<IEnumerable<dynamic>> GetQuarantineStockAsync()
        {
            var results = new List<dynamic>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT ist.ProductID, p.SKU, p.ProductName, ist.ShelfID, s.ShelfCode, ist.CurrentQty, p.StandardCost AS UnitPrice
                FROM InventoryStocks ist
                INNER JOIN Products p ON ist.ProductID = p.ProductID
                INNER JOIN Shelves s ON ist.ShelfID = s.ShelfID
                WHERE s.ShelfType = 'Quarantine' AND ist.CurrentQty > 0
                ORDER BY p.SKU", connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new
                {
                    ProductID = reader.GetInt32(0),
                    SKU = reader.GetString(1),
                    ProductName = reader.GetString(2),
                    ShelfID = reader.GetInt32(3),
                    ShelfCode = reader.GetString(4),
                    CurrentQty = reader.GetDecimal(5),
                    UnitPrice = reader.GetDecimal(6)
                });
            }
            return results;
        }

        public async Task<bool> SupplierAuthorizeReturnAsync(int returnId, string rmaNumber, string carrier, string trackingNumber, string? creditNoteNo = null, decimal? creditAmount = null, string? replacementSKU = null, decimal? replacementQty = null, string? replacementTracking = null)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                UPDATE SupplierReturns 
                SET RMANumber = @RMANumber, 
                    Carrier = COALESCE(@Carrier, Carrier), 
                    TrackingNumber = COALESCE(@TrackingNumber, TrackingNumber), 
                    CreditNoteNo = @CreditNoteNo,
                    CreditAmount = @CreditAmount,
                    CreditStatus = CASE WHEN @CreditNoteNo IS NOT NULL AND LEN(@CreditNoteNo) > 0 THEN 'Issued' ELSE CreditStatus END,
                    ReplacementSKU = @ReplacementSKU,
                    ReplacementQty = @ReplacementQty,
                    ReplacementTracking = @ReplacementTracking,
                    Status = 'Shipped',
                    UpdatedAt = GETDATE() 
                WHERE ReturnID = @ReturnID AND Status = 'Draft'", connection);
            command.Parameters.AddWithValue("@ReturnID", returnId);
            command.Parameters.AddWithValue("@RMANumber", rmaNumber);
            command.Parameters.AddWithValue("@Carrier", string.IsNullOrWhiteSpace(carrier) ? (object)DBNull.Value : carrier);
            command.Parameters.AddWithValue("@TrackingNumber", string.IsNullOrWhiteSpace(trackingNumber) ? (object)DBNull.Value : trackingNumber);
            command.Parameters.AddWithValue("@CreditNoteNo", string.IsNullOrWhiteSpace(creditNoteNo) ? (object)DBNull.Value : creditNoteNo);
            command.Parameters.AddWithValue("@CreditAmount", creditAmount.HasValue ? (object)creditAmount.Value : DBNull.Value);
            command.Parameters.AddWithValue("@ReplacementSKU", string.IsNullOrWhiteSpace(replacementSKU) ? (object)DBNull.Value : replacementSKU);
            command.Parameters.AddWithValue("@ReplacementQty", replacementQty.HasValue ? (object)replacementQty.Value : DBNull.Value);
            command.Parameters.AddWithValue("@ReplacementTracking", string.IsNullOrWhiteSpace(replacementTracking) ? (object)DBNull.Value : replacementTracking);

            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> RejectSupplierReturnAsync(int returnId, string rejectionReason)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    UPDATE SupplierReturns 
                    SET Status = 'Cancelled', RejectionReason = @RejectionReason, UpdatedAt = GETDATE()
                    WHERE ReturnID = @ReturnID AND Status = 'Draft'", connection);

                command.Parameters.AddWithValue("@ReturnID", returnId);
                command.Parameters.AddWithValue("@RejectionReason", rejectionReason);

                await connection.OpenAsync();
                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error rejecting Supplier Return in repository.", ex);
            }
        }
    }
}

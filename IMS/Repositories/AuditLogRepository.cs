using IMS.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly string _connectionString;

        public AuditLogRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<IReadOnlyList<AuditLogDto>> GetStockMovementAuditAsync(AuditLogFilterDto filter)
        {
            var results = new List<AuditLogDto>();
            var sql = new StringBuilder(@"
                SELECT * FROM dbo.vw_StockMovementReport
                WHERE 1=1");
            
            var parameters = new List<SqlParameter>();

            if (filter.DateFrom.HasValue)
            {
                sql.Append(" AND MovementDate >= @DateFrom");
                parameters.Add(new SqlParameter("@DateFrom", filter.DateFrom.Value));
            }

            if (filter.DateTo.HasValue)
            {
                sql.Append(" AND MovementDate <= @DateTo");
                // Set to end of day
                parameters.Add(new SqlParameter("@DateTo", filter.DateTo.Value.Date.AddDays(1).AddTicks(-1)));
            }

            if (!string.IsNullOrEmpty(filter.MovementType))
            {
                sql.Append(" AND MovementType = @MovementType");
                parameters.Add(new SqlParameter("@MovementType", filter.MovementType));
            }

            if (!string.IsNullOrEmpty(filter.ReferenceNo))
            {
                sql.Append(" AND ReferenceNo LIKE @ReferenceNo");
                parameters.Add(new SqlParameter("@ReferenceNo", "%" + filter.ReferenceNo + "%"));
            }

            // Note: PerformedBy in view is Name, filter might be UserID if we had it. 
            // For now, if we pass PerformedByUserID, we need to join Users table again or filter by name if that's all we have.
            // Let's assume filter.PerformedByUserID is used.
            if (filter.PerformedByUserID.HasValue)
            {
                sql.Append(" AND MovementID IN (SELECT MovementID FROM StockMovements WHERE PerformedBy = @UserID)");
                parameters.Add(new SqlParameter("@UserID", filter.PerformedByUserID.Value));
            }

            if (filter.ProductID.HasValue)
            {
                sql.Append(" AND MovementID IN (SELECT MovementID FROM StockMovements WHERE ProductID = @ProductID)");
                parameters.Add(new SqlParameter("@ProductID", filter.ProductID.Value));
            }

            sql.Append(" ORDER BY MovementDate DESC");

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(sql.ToString(), connection);
                command.Parameters.AddRange(parameters.ToArray());

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new AuditLogDto
                    {
                        MovementID = reader.GetInt32("MovementID"),
                        MovementDate = reader.GetDateTime("MovementDate"),
                        SKU = reader.GetString("SKU"),
                        ProductName = reader.GetString("ProductName"),
                        WarehouseName = reader.GetString("WarehouseName"),
                        ShelfCode = reader.GetString("ShelfCode"),
                        MovementType = reader.GetString("MovementType"),
                        Quantity = reader.GetDecimal("Quantity"),
                        BeforeQty = reader.GetDecimal("BeforeQty"),
                        AfterQty = reader.GetDecimal("AfterQty"),
                        ReferenceType = reader.IsDBNull("ReferenceType") ? "" : reader.GetString("ReferenceType"),
                        ReferenceNo = reader.IsDBNull("ReferenceNo") ? "" : reader.GetString("ReferenceNo"),
                        PerformedBy = reader.IsDBNull("PerformedBy") ? "System" : reader.GetString("PerformedBy"),
                        Remarks = reader.IsDBNull("Remarks") ? "" : reader.GetString("Remarks")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching stock movement audit logs.", ex);
            }

            return results;
        }

        public async Task<AuditLogSummaryDto> GetAuditSummaryAsync(AuditLogFilterDto filter)
        {
            var summary = new AuditLogSummaryDto();
            
            // Re-using filter logic for summary counts
            var sql = new StringBuilder(@"
                SELECT 
                    COUNT(*) as TotalCount,
                    SUM(CASE WHEN Quantity > 0 AND MovementType IN ('StockIn', 'AdjustmentIn', 'TransferIn', 'ReturnIn', 'ProductionReturn') THEN Quantity ELSE 0 END) as TotalIn,
                    SUM(CASE WHEN Quantity < 0 OR MovementType IN ('StockOut', 'AdjustmentOut', 'TransferOut', 'DamageOut', 'ProductionIssue') THEN ABS(Quantity) ELSE 0 END) as TotalOut,
                    SUM(CASE WHEN MovementType LIKE 'Adjustment%' THEN ABS(Quantity) ELSE 0 END) as TotalAdj,
                    SUM(CASE WHEN MovementType LIKE 'Transfer%' THEN ABS(Quantity) ELSE 0 END) as TotalTrans
                FROM StockMovements
                WHERE 1=1");

            var parameters = new List<SqlParameter>();

            if (filter.DateFrom.HasValue)
            {
                sql.Append(" AND CreatedAt >= @DateFrom");
                parameters.Add(new SqlParameter("@DateFrom", filter.DateFrom.Value));
            }

            if (filter.DateTo.HasValue)
            {
                sql.Append(" AND CreatedAt <= @DateTo");
                parameters.Add(new SqlParameter("@DateTo", filter.DateTo.Value.Date.AddDays(1).AddTicks(-1)));
            }

            if (filter.ProductID.HasValue)
            {
                sql.Append(" AND ProductID = @ProductID");
                parameters.Add(new SqlParameter("@ProductID", filter.ProductID.Value));
            }

            if (filter.PerformedByUserID.HasValue)
            {
                sql.Append(" AND PerformedBy = @UserID");
                parameters.Add(new SqlParameter("@UserID", filter.PerformedByUserID.Value));
            }

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(sql.ToString(), connection);
                command.Parameters.AddRange(parameters.ToArray());

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    summary.TotalMovements = reader.IsDBNull("TotalCount") ? 0 : reader.GetInt32("TotalCount");
                    summary.TotalStockIn = reader.IsDBNull("TotalIn") ? 0 : reader.GetDecimal("TotalIn");
                    summary.TotalStockOut = reader.IsDBNull("TotalOut") ? 0 : reader.GetDecimal("TotalOut");
                    summary.TotalAdjustments = reader.IsDBNull("TotalAdj") ? 0 : reader.GetDecimal("TotalAdj");
                    summary.TotalTransfers = reader.IsDBNull("TotalTrans") ? 0 : reader.GetDecimal("TotalTrans");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching audit summary.", ex);
            }

            return summary;
        }

        public async Task<IReadOnlyList<DropdownOptionDto>> GetMovementTypesAsync()
        {
            var types = new List<string> { "StockIn", "StockOut", "Reserve", "Unreserve", "TransferOut", "TransferIn", "AdjustmentIn", "AdjustmentOut", "ReturnIn", "DamageOut", "ProductionIssue", "ProductionReturn" };
            return types.Select(t => new DropdownOptionDto { Value = t, Text = t }).ToList();
        }

        public async Task<IReadOnlyList<DropdownOptionDto>> GetProductsAsync()
        {
            var results = new List<DropdownOptionDto>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT ProductID, SKU + ' - ' + ProductName as DisplayName FROM Products ORDER BY SKU", connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new DropdownOptionDto { Value = reader.GetInt32(0).ToString(), Text = reader.GetString(1) });
            }
            return results;
        }

        public async Task<IReadOnlyList<DropdownOptionDto>> GetUsersAsync()
        {
            var results = new List<DropdownOptionDto>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT UserID, FullName FROM Users ORDER BY FullName", connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new DropdownOptionDto { Value = reader.GetInt32(0).ToString(), Text = reader.GetString(1) });
            }
            return results;
        }
    }
}

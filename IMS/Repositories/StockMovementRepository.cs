using IMS.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public class StockMovementRepository : IStockMovementRepository
    {
        private readonly string _connectionString;

        public StockMovementRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<IEnumerable<StockMovementDto>> GetRecentMovementsAsync(int limit = 100)
        {
            var results = new List<StockMovementDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand($@"
                    SELECT TOP {limit} * 
                    FROM dbo.vw_StockMovementReport 
                    ORDER BY MovementDate DESC", connection);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new StockMovementDto
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
                        PerformedBy = reader.IsDBNull("PerformedBy") ? "" : reader.GetString("PerformedBy"),
                        Remarks = reader.IsDBNull("Remarks") ? "" : reader.GetString("Remarks")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching stock movements.", ex);
            }

            return results;
        }

        public async Task<IEnumerable<StockMovementTrendDto>> GetMovementTrendAsync(int days = 7)
        {
            var results = new List<StockMovementTrendDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT
                        CAST(MovementDate AS DATE) AS MovementDay,
                        SUM(CASE WHEN MovementType IN ('StockIn', 'TransferIn', 'AdjustmentIn', 'ReturnIn', 'ProductionReturn') OR MovementType LIKE '%In'
                            THEN ABS(Quantity) ELSE 0 END) AS InboundQty,
                        SUM(CASE WHEN MovementType IN ('StockOut', 'Dispatch', 'TransferOut', 'AdjustmentOut', 'DamageOut', 'ProductionIssue') OR MovementType LIKE '%Out'
                            THEN ABS(Quantity) ELSE 0 END) AS OutboundQty
                    FROM dbo.vw_StockMovementReport
                    WHERE MovementDate >= DATEADD(DAY, -(@Days - 1), CAST(GETDATE() AS DATE))
                    GROUP BY CAST(MovementDate AS DATE)
                    ORDER BY MovementDay", connection);
                command.Parameters.AddWithValue("@Days", days);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new StockMovementTrendDto
                    {
                        Date = reader.GetDateTime("MovementDay"),
                        InboundQty = reader.GetDecimal("InboundQty"),
                        OutboundQty = reader.GetDecimal("OutboundQty")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching stock movement trend.", ex);
            }

            return results;
        }
    }
}

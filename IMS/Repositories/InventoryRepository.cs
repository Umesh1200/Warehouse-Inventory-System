using IMS.Models.DTOs;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IMS.Repositories
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly string _connectionString;

        public InventoryRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<(IEnumerable<InventoryOverviewDto> Items, int TotalCount)> GetInventoryOverviewAsync(string? warehouse, string? category, string? status, int pageNumber, int pageSize)
        {
            var results = new List<InventoryOverviewDto>();
            int totalCount = 0;

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var whereClause = @"
                    WHERE (@Warehouse IS NULL OR @Warehouse = 'All Warehouses' OR WarehouseName = @Warehouse)
                      AND (@Category IS NULL OR @Category = 'All Categories' OR CategoryName = @Category)
                      AND (@Status IS NULL OR @Status = 'All Statuses' OR StockStatus = @Status)";

                var countQuery = $"SELECT COUNT(*) FROM dbo.vw_InventoryOverview {whereClause}";
                var dataQuery = $@"
                    SELECT * FROM dbo.vw_InventoryOverview 
                    {whereClause}
                    ORDER BY ProductName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
                
                await connection.OpenAsync();

                // Get Total Count
                using (var countCmd = new SqlCommand(countQuery, connection))
                {
                    countCmd.Parameters.AddWithValue("@Warehouse", (object?)warehouse ?? DBNull.Value);
                    countCmd.Parameters.AddWithValue("@Category", (object?)category ?? DBNull.Value);
                    countCmd.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);
                    var countResult = await countCmd.ExecuteScalarAsync();
                    totalCount = countResult != null ? (int)countResult : 0;
                }

                // Get Paginated Data
                using (var dataCmd = new SqlCommand(dataQuery, connection))
                {
                    dataCmd.Parameters.AddWithValue("@Warehouse", (object?)warehouse ?? DBNull.Value);
                    dataCmd.Parameters.AddWithValue("@Category", (object?)category ?? DBNull.Value);
                    dataCmd.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);
                    dataCmd.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);
                    dataCmd.Parameters.AddWithValue("@PageSize", pageSize);

                    using var reader = await dataCmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        results.Add(new InventoryOverviewDto
                        {
                            ProductID = reader.GetInt32("ProductID"),
                            SKU = reader.GetString("SKU"),
                            ProductName = reader.GetString("ProductName"),
                            CategoryName = reader.IsDBNull("CategoryName") ? "" : reader.GetString("CategoryName"),
                            ProductType = reader.IsDBNull("ProductType") ? "" : reader.GetString("ProductType"),
                            WarehouseName = reader.IsDBNull("WarehouseName") ? "" : reader.GetString("WarehouseName"),
                            ShelfCode = reader.IsDBNull("ShelfCode") ? "" : reader.GetString("ShelfCode"),
                            ShelfType = reader.IsDBNull("ShelfType") ? "" : reader.GetString("ShelfType"),
                            CurrentQty = reader.GetDecimal("CurrentQty"),
                            ReservedQty = reader.GetDecimal("ReservedQty"),
                            QCHoldQty = reader.GetDecimal("QCHoldQty"),
                            DamagedQty = reader.GetDecimal("DamagedQty"),
                            AvailableQty = reader.GetDecimal("AvailableQty"),
                            StockStatus = reader.GetString("StockStatus"),
                            ReorderLevel = reader.GetDecimal("ReorderLevel"),
                            StandardCost = reader.GetDecimal("StandardCost"),
                            TotalValue = reader.GetDecimal("TotalValue")
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching inventory overview.", ex);
            }

            return (results, totalCount);
        }
    }
}

using IMS.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public class ProductStorageRuleRepository : IProductStorageRuleRepository
    {
        private readonly string _connectionString;

        public ProductStorageRuleRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<IEnumerable<ProductStorageRuleDto>> GetRulesByProductIDAsync(int productId)
        {
            var results = new List<ProductStorageRuleDto>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT 
                        psr.RuleID,
                        psr.ProductID,
                        psr.AllowedShelfType,
                        psr.MaxStackQty,
                        psr.RequiresQCHold,
                        psr.IsFragile,
                        psr.IsHazardous,
                        psr.PreferredWarehouseID,
                        w.WarehouseName AS PreferredWarehouseName,
                        psr.PreferredRackID,
                        rk.RackName AS PreferredRackName,
                        psr.PreferredShelfID,
                        s.ShelfCode AS PreferredShelfCode,
                        psr.IsActive,
                        psr.CreatedAt
                    FROM ProductStorageRules psr
                    LEFT JOIN Warehouses w ON psr.PreferredWarehouseID = w.WarehouseID
                    LEFT JOIN Racks rk ON psr.PreferredRackID = rk.RackID
                    LEFT JOIN Shelves s ON psr.PreferredShelfID = s.ShelfID
                    WHERE psr.ProductID = @ProductID AND psr.IsActive = 1
                    ORDER BY psr.AllowedShelfType ASC", connection);

                command.Parameters.AddWithValue("@ProductID", productId);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(MapFromReader(reader));
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error loading product storage rules.", ex);
            }
            return results;
        }

        public async Task<int> SaveRuleAsync(ProductStorageRuleSaveDto rule)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    IF @RuleID > 0
                    BEGIN
                        UPDATE ProductStorageRules
                        SET AllowedShelfType = @AllowedShelfType,
                            MaxStackQty = @MaxStackQty,
                            RequiresQCHold = @RequiresQCHold,
                            IsFragile = @IsFragile,
                            IsHazardous = @IsHazardous,
                            PreferredWarehouseID = @PreferredWarehouseID,
                            PreferredRackID = @PreferredRackID,
                            PreferredShelfID = @PreferredShelfID,
                            IsActive = 1
                        WHERE RuleID = @RuleID;
                        SELECT @RuleID;
                    END
                    ELSE
                    BEGIN
                        INSERT INTO ProductStorageRules (ProductID, AllowedShelfType, MaxStackQty, RequiresQCHold, IsFragile, IsHazardous, PreferredWarehouseID, PreferredRackID, PreferredShelfID, IsActive, CreatedAt)
                        VALUES (@ProductID, @AllowedShelfType, @MaxStackQty, @RequiresQCHold, @IsFragile, @IsHazardous, @PreferredWarehouseID, @PreferredRackID, @PreferredShelfID, 1, GETDATE());
                        SELECT SCOPE_IDENTITY();
                    END", connection);

                command.Parameters.AddWithValue("@RuleID", rule.RuleID);
                command.Parameters.AddWithValue("@ProductID", rule.ProductID);
                command.Parameters.AddWithValue("@AllowedShelfType", rule.AllowedShelfType);
                command.Parameters.AddWithValue("@MaxStackQty", rule.MaxStackQty ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@RequiresQCHold", rule.RequiresQCHold);
                command.Parameters.AddWithValue("@IsFragile", rule.IsFragile);
                command.Parameters.AddWithValue("@IsHazardous", rule.IsHazardous);
                command.Parameters.AddWithValue("@PreferredWarehouseID", rule.PreferredWarehouseID ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PreferredRackID", rule.PreferredRackID ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PreferredShelfID", rule.PreferredShelfID ?? (object)DBNull.Value);

                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                throw new Exception("Error saving product storage rule.", ex);
            }
        }

        public async Task<bool> DeactivateRuleAsync(int ruleId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand("UPDATE ProductStorageRules SET IsActive = 0 WHERE RuleID = @RuleID", connection);
                command.Parameters.AddWithValue("@RuleID", ruleId);

                await connection.OpenAsync();
                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error deactivating storage rule.", ex);
            }
        }

        public async Task<bool> CheckRuleExistsAsync(int productId, string allowedShelfType, int? excludeRuleId = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT COUNT(*) 
                    FROM ProductStorageRules 
                    WHERE ProductID = @ProductID 
                      AND AllowedShelfType = @AllowedShelfType 
                      AND IsActive = 1
                      AND (@ExcludeRuleID IS NULL OR RuleID <> @ExcludeRuleID)", connection);

                command.Parameters.AddWithValue("@ProductID", productId);
                command.Parameters.AddWithValue("@AllowedShelfType", allowedShelfType);
                command.Parameters.AddWithValue("@ExcludeRuleID", excludeRuleId ?? (object)DBNull.Value);

                await connection.OpenAsync();
                var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                return count > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error checking storage rule uniqueness.", ex);
            }
        }

        public async Task<ProductStorageRuleDto?> GetRuleByIdAsync(int ruleId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT 
                        psr.RuleID,
                        psr.ProductID,
                        psr.AllowedShelfType,
                        psr.MaxStackQty,
                        psr.RequiresQCHold,
                        psr.IsFragile,
                        psr.IsHazardous,
                        psr.PreferredWarehouseID,
                        w.WarehouseName AS PreferredWarehouseName,
                        psr.PreferredRackID,
                        rk.RackName AS PreferredRackName,
                        psr.PreferredShelfID,
                        s.ShelfCode AS PreferredShelfCode,
                        psr.IsActive,
                        psr.CreatedAt
                    FROM ProductStorageRules psr
                    LEFT JOIN Warehouses w ON psr.PreferredWarehouseID = w.WarehouseID
                    LEFT JOIN Racks rk ON psr.PreferredRackID = rk.RackID
                    LEFT JOIN Shelves s ON psr.PreferredShelfID = s.ShelfID
                    WHERE psr.RuleID = @RuleID", connection);

                command.Parameters.AddWithValue("@RuleID", ruleId);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapFromReader(reader);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving storage rule.", ex);
            }
            return null;
        }

        private ProductStorageRuleDto MapFromReader(SqlDataReader reader)
        {
            return new ProductStorageRuleDto
            {
                RuleID = reader.GetInt32("RuleID"),
                ProductID = reader.GetInt32("ProductID"),
                AllowedShelfType = reader.GetString("AllowedShelfType"),
                MaxStackQty = reader.IsDBNull("MaxStackQty") ? (decimal?)null : reader.GetDecimal("MaxStackQty"),
                RequiresQCHold = reader.GetBoolean("RequiresQCHold"),
                IsFragile = reader.GetBoolean("IsFragile"),
                IsHazardous = reader.GetBoolean("IsHazardous"),
                PreferredWarehouseID = reader.IsDBNull("PreferredWarehouseID") ? (int?)null : reader.GetInt32("PreferredWarehouseID"),
                PreferredWarehouseName = reader.IsDBNull("PreferredWarehouseName") ? "-" : reader.GetString("PreferredWarehouseName"),
                PreferredRackID = reader.IsDBNull("PreferredRackID") ? (int?)null : reader.GetInt32("PreferredRackID"),
                PreferredRackName = reader.IsDBNull("PreferredRackName") ? "-" : reader.GetString("PreferredRackName"),
                PreferredShelfID = reader.IsDBNull("PreferredShelfID") ? (int?)null : reader.GetInt32("PreferredShelfID"),
                PreferredShelfCode = reader.IsDBNull("PreferredShelfCode") ? "-" : reader.GetString("PreferredShelfCode"),
                IsActive = reader.GetBoolean("IsActive"),
                CreatedAt = reader.GetDateTime("CreatedAt")
            };
        }
    }
}

using IMS.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public class BomRepository : IBomRepository
    {
        private readonly string _connectionString;

        public BomRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<IEnumerable<BomDto>> GetAllBomsAsync()
        {
            var results = new List<BomDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT b.*, 
                           p.SKU AS FinishedProductSKU, 
                           p.ProductName AS FinishedProductName,
                           u.FullName AS CreatedByName,
                           items.ItemCount,
                           items.TotalRequiredQty
                    FROM BOM b
                    INNER JOIN Products p ON b.ProductID = p.ProductID
                    LEFT JOIN Users u ON b.CreatedBy = u.UserID
                    LEFT JOIN (
                        SELECT BOMID, 
                               COUNT(*) AS ItemCount, 
                               SUM(RequiredQty) AS TotalRequiredQty
                        FROM BOMItems
                        GROUP BY BOMID
                    ) items ON b.BOMID = items.BOMID
                    ORDER BY b.CreatedAt DESC", connection);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new BomDto
                    {
                        BOMID = reader.GetInt32("BOMID"),
                        BOMVersion = reader.GetString("BOMVersion"),
                        ProductID = reader.GetInt32("ProductID"),
                        FinishedProductSKU = reader.GetString("FinishedProductSKU"),
                        FinishedProductName = reader.GetString("FinishedProductName"),
                        EffectiveDate = reader.IsDBNull("EffectiveDate") ? (DateTime?)null : reader.GetDateTime("EffectiveDate"),
                        Status = reader.GetString("Status"),
                        CreatedBy = reader.GetInt32("CreatedBy"),
                        CreatedByName = reader.IsDBNull("CreatedByName") ? "" : reader.GetString("CreatedByName"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        ItemCount = reader.IsDBNull("ItemCount") ? 0 : reader.GetInt32("ItemCount"),
                        TotalRequiredQty = reader.IsDBNull("TotalRequiredQty") ? 0 : reader.GetDecimal("TotalRequiredQty")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching BOMs.", ex);
            }

            return results;
        }

        public async Task<IEnumerable<BomItemDto>> GetBomItemsAsync(int bomId)
        {
            var results = new List<BomItemDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT bi.*, p.SKU AS RawMaterialSKU, p.ProductName AS RawMaterialName, p.ProductType AS RawMaterialType, un.UnitCode
                    FROM BOMItems bi
                    INNER JOIN Products p ON bi.RawMaterialID = p.ProductID
                    LEFT JOIN Units un ON p.UnitID = un.UnitID
                    WHERE bi.BOMID = @BOMID", connection);

                command.Parameters.AddWithValue("@BOMID", bomId);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new BomItemDto
                    {
                        BOMItemID = reader.GetInt32("BOMItemID"),
                        BOMID = reader.GetInt32("BOMID"),
                        RawMaterialID = reader.GetInt32("RawMaterialID"),
                        RawMaterialSKU = reader.GetString("RawMaterialSKU"),
                        RawMaterialName = reader.GetString("RawMaterialName"),
                        RequiredQty = reader.GetDecimal("RequiredQty"),
                        WastagePercent = reader.IsDBNull("WastagePercent") ? 0 : reader.GetDecimal("WastagePercent"),
                        UnitCode = reader.IsDBNull("UnitCode") ? "" : reader.GetString("UnitCode"),
                        RawMaterialType = reader.IsDBNull("RawMaterialType") ? "" : reader.GetString("RawMaterialType")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching BOM items.", ex);
            }

            return results;
        }

        public async Task<int> CreateBomAsync(BomCreateDto bom)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Process Finished Good if it's completely new
                if (bom.IsNewFinishedProduct || (bom.ProductID == 0 && !string.IsNullOrWhiteSpace(bom.NewFinishedSKU)))
                {
                    // Check if Finished Good SKU already exists in Products
                    using var checkFinishedCmd = new SqlCommand("SELECT ProductID FROM Products WHERE SKU = @SKU", connection, transaction);
                    checkFinishedCmd.Parameters.AddWithValue("@SKU", bom.NewFinishedSKU);
                    var existingFinishedIdObj = await checkFinishedCmd.ExecuteScalarAsync();
                    if (existingFinishedIdObj != null)
                    {
                        bom.ProductID = Convert.ToInt32(existingFinishedIdObj);
                    }
                    else
                    {
                        // Insert finished good product
                        using var insertFinishedCmd = new SqlCommand(@"
                            INSERT INTO Products (
                                SKU, ProductName, CategoryID, UnitID, ProductType, 
                                ReorderLevel, StandardCost, SellingPrice, WeightKg, Status, Description
                            )
                            VALUES (
                                @SKU, @ProductName, @CategoryID, @UnitID, 'Finished Goods', 
                                0, @StandardCost, 0, 0, 'Active', @Description
                            );
                            SELECT SCOPE_IDENTITY();", connection, transaction);

                        insertFinishedCmd.Parameters.AddWithValue("@SKU", bom.NewFinishedSKU);
                        insertFinishedCmd.Parameters.AddWithValue("@ProductName", bom.NewFinishedName);
                        insertFinishedCmd.Parameters.AddWithValue("@CategoryID", bom.NewFinishedCategoryID);
                        insertFinishedCmd.Parameters.AddWithValue("@UnitID", bom.NewFinishedUnitID);
                        insertFinishedCmd.Parameters.AddWithValue("@StandardCost", bom.NewFinishedStandardCost);
                        insertFinishedCmd.Parameters.AddWithValue("@Description", bom.NewFinishedNotes);

                        var newFinishedIdObj = await insertFinishedCmd.ExecuteScalarAsync();
                        bom.ProductID = Convert.ToInt32(newFinishedIdObj);
                    }
                }

                // 2. Lock CategoryID for raw materials to the 'Raw Material' Category ID in the database
                int rawMaterialCategoryId = 4; // default fallback seed value
                using (var catCmd = new SqlCommand("SELECT CategoryID FROM Categories WHERE CategoryName = 'Raw Material'", connection, transaction))
                {
                    var catIdObj = await catCmd.ExecuteScalarAsync();
                    if (catIdObj != null)
                    {
                        rawMaterialCategoryId = Convert.ToInt32(catIdObj);
                    }
                }

                // 3. Process items that are new materials
                foreach (var item in bom.Items)
                {
                    if (item.ProductID == 0)
                    {
                        // Check if SKU already exists in Products
                        using var checkCmd = new SqlCommand("SELECT ProductID FROM Products WHERE SKU = @SKU", connection, transaction);
                        checkCmd.Parameters.AddWithValue("@SKU", item.NewMaterialSKU);
                        var existingIdObj = await checkCmd.ExecuteScalarAsync();
                        if (existingIdObj != null)
                        {
                            item.ProductID = Convert.ToInt32(existingIdObj);
                        }
                        else
                        {
                            // Insert product of type 'Raw Material'
                            using var insertProdCmd = new SqlCommand(@"
                                INSERT INTO Products (
                                    SKU, ProductName, CategoryID, UnitID, ProductType, 
                                    ReorderLevel, StandardCost, SellingPrice, WeightKg, Status, Description
                                )
                                VALUES (
                                    @SKU, @ProductName, @CategoryID, @UnitID, 'Raw Material', 
                                    0, @StandardCost, 0, 0, 'Active', @Description
                                );
                                SELECT SCOPE_IDENTITY();", connection, transaction);

                            insertProdCmd.Parameters.AddWithValue("@SKU", item.NewMaterialSKU);
                            insertProdCmd.Parameters.AddWithValue("@ProductName", item.NewMaterialName);
                            insertProdCmd.Parameters.AddWithValue("@CategoryID", rawMaterialCategoryId); // Locked to 'Raw Material' Category
                            insertProdCmd.Parameters.AddWithValue("@UnitID", item.NewMaterialUnitID);
                            insertProdCmd.Parameters.AddWithValue("@StandardCost", item.NewMaterialStandardCost);
                            insertProdCmd.Parameters.AddWithValue("@Description", item.NewMaterialNotes);

                            var newProdIdObj = await insertProdCmd.ExecuteScalarAsync();
                            item.ProductID = Convert.ToInt32(newProdIdObj);
                        }
                    }
                }

                // 4. Insert BOM Header
                // Check if Version already exists
                using var verCheckCmd = new SqlCommand("SELECT COUNT(*) FROM BOM WHERE BOMVersion = @BOMVersion", connection, transaction);
                verCheckCmd.Parameters.AddWithValue("@BOMVersion", bom.BOMVersion);
                int verCount = (int)(await verCheckCmd.ExecuteScalarAsync() ?? 0);
                if (verCount > 0)
                {
                    throw new Exception($"BOM version '{bom.BOMVersion}' already exists.");
                }

                using var bomCmd = new SqlCommand(@"
                    INSERT INTO BOM (BOMVersion, ProductID, EffectiveDate, Status, CreatedBy, CreatedAt)
                    VALUES (@BOMVersion, @ProductID, @EffectiveDate, @Status, @CreatedBy, GETDATE());
                    SELECT SCOPE_IDENTITY();", connection, transaction);

                bomCmd.Parameters.AddWithValue("@BOMVersion", bom.BOMVersion);
                bomCmd.Parameters.AddWithValue("@ProductID", bom.ProductID);
                bomCmd.Parameters.AddWithValue("@EffectiveDate", bom.EffectiveDate ?? (object)DBNull.Value);
                bomCmd.Parameters.AddWithValue("@Status", bom.Status);
                bomCmd.Parameters.AddWithValue("@CreatedBy", bom.CreatedBy);

                var bomIdObj = await bomCmd.ExecuteScalarAsync();
                int bomId = Convert.ToInt32(bomIdObj);

                // 5. Insert BOM Items
                foreach (var item in bom.Items)
                {
                    using var itemCmd = new SqlCommand(@"
                        INSERT INTO BOMItems (BOMID, RawMaterialID, RequiredQty, WastagePercent)
                        VALUES (@BOMID, @RawMaterialID, @RequiredQty, @WastagePercent)", connection, transaction);

                    itemCmd.Parameters.AddWithValue("@BOMID", bomId);
                    itemCmd.Parameters.AddWithValue("@RawMaterialID", item.ProductID);
                    itemCmd.Parameters.AddWithValue("@RequiredQty", item.RequiredQty);
                    itemCmd.Parameters.AddWithValue("@WastagePercent", item.WastagePercent);

                    await itemCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return bomId;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception("Error occurred during BOM creation inside transaction.", ex);
            }
        }
    }
}

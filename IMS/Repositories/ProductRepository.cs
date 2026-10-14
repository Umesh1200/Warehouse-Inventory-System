using IMS.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace IMS.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");

            // Auto-migration to ensure Products, Categories, and Units have required tables/columns
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                using var command = new SqlCommand(@"
                    IF NOT EXISTS (
                        SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'Products' AND COLUMN_NAME = 'Description'
                    )
                    BEGIN
                        ALTER TABLE Products ADD Description NVARCHAR(500) NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Categories')
                    BEGIN
                        CREATE TABLE Categories (
                            CategoryID INT IDENTITY(1,1) PRIMARY KEY,
                            CategoryName NVARCHAR(100) NOT NULL UNIQUE,
                            Description NVARCHAR(255) NULL,
                            IsActive BIT DEFAULT 1
                        );
                    END
                    ELSE IF NOT EXISTS (
                        SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'Categories' AND COLUMN_NAME = 'IsActive'
                    )
                    BEGIN
                        ALTER TABLE Categories ADD IsActive BIT DEFAULT 1;
                    END

                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Units')
                    BEGIN
                        CREATE TABLE Units (
                            UnitID INT IDENTITY(1,1) PRIMARY KEY,
                            UnitName NVARCHAR(50) NOT NULL UNIQUE,
                            UnitCode NVARCHAR(10) NOT NULL UNIQUE,
                            IsActive BIT DEFAULT 1
                        );
                    END
                    ELSE IF NOT EXISTS (
                        SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'Units' AND COLUMN_NAME = 'IsActive'
                    )
                    BEGIN
                        ALTER TABLE Units ADD IsActive BIT DEFAULT 1;
                    END", connection);
                command.ExecuteNonQuery();
            }
            catch { /* Ignore database connectivity errors here, handled in operations */ }
        }

        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
        {
            var products = new List<ProductDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT p.*, c.CategoryName, u.UnitCode
                    FROM Products p
                    LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                    LEFT JOIN Units u ON p.UnitID = u.UnitID
                    ORDER BY p.SKU", connection);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    products.Add(new ProductDto
                    {
                        ProductID = reader.GetInt32("ProductID"),
                        SKU = reader.GetString("SKU"),
                        ProductName = reader.GetString("ProductName"),
                        CategoryID = reader.IsDBNull("CategoryID") ? 0 : reader.GetInt32("CategoryID"),
                        CategoryName = reader.IsDBNull("CategoryName") ? "" : reader.GetString("CategoryName"),
                        UnitID = reader.IsDBNull("UnitID") ? 0 : reader.GetInt32("UnitID"),
                        UnitCode = reader.IsDBNull("UnitCode") ? "" : reader.GetString("UnitCode"),
                        ProductType = reader.IsDBNull("ProductType") ? "" : reader.GetString("ProductType"),
                        ReorderLevel = reader.IsDBNull("ReorderLevel") ? 0 : reader.GetDecimal("ReorderLevel"),
                        StandardCost = reader.IsDBNull("StandardCost") ? 0 : reader.GetDecimal("StandardCost"),
                        SellingPrice = reader.IsDBNull("SellingPrice") ? 0 : reader.GetDecimal("SellingPrice"),
                        WeightKg = reader.IsDBNull("WeightKg") ? 0 : reader.GetDecimal("WeightKg"),
                        Status = reader.IsDBNull("Status") ? "Active" : reader.GetString("Status"),
                        Description = reader.IsDBNull("Description") ? "" : reader.GetString("Description"),
                        ImageUrl = reader.IsDBNull("ImageUrl") ? "" : reader.GetString("ImageUrl")
                    });
                }
            }
            catch (Exception ex)
            {
                // In a real app, log the exception
                throw new Exception("Database error occurred while fetching products.", ex);
            }

            return products;
        }

        public async Task<int> CreateProductAsync(ProductCreateDto product)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    INSERT INTO Products (
                        SKU, ProductName, CategoryID, UnitID, ProductType, 
                        ReorderLevel, StandardCost, SellingPrice, WeightKg, Dimensions, Status, Description, ImageUrl
                    )
                    VALUES (
                        @SKU, @ProductName, @CategoryID, @UnitID, @ProductType, 
                        @ReorderLevel, @StandardCost, @SellingPrice, @WeightKg, @Dimensions, @Status, @Description, @ImageUrl
                    );
                    SELECT SCOPE_IDENTITY();", connection);

                command.Parameters.AddWithValue("@SKU", product.SKU);
                command.Parameters.AddWithValue("@ProductName", product.ProductName);
                command.Parameters.AddWithValue("@CategoryID", product.CategoryID);
                command.Parameters.AddWithValue("@UnitID", product.UnitID);
                command.Parameters.AddWithValue("@ProductType", product.ProductType);
                command.Parameters.AddWithValue("@ReorderLevel", product.ReorderLevel);
                command.Parameters.AddWithValue("@StandardCost", product.StandardCost);
                command.Parameters.AddWithValue("@SellingPrice", product.SellingPrice);
                command.Parameters.AddWithValue("@WeightKg", product.WeightKg);
                command.Parameters.AddWithValue("@Dimensions", product.Dimensions ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Status", product.Status);
                command.Parameters.AddWithValue("@Description", product.Description ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ImageUrl", product.ImageUrl ?? (object)DBNull.Value);

                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                throw new Exception("Product SKU already exists.");
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while creating product.", ex);
            }
        }

        public async Task<IEnumerable<dynamic>> GetCategoriesAsync()
        {
            var results = new List<dynamic>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT CategoryID, CategoryName FROM Categories WHERE IsActive = 1 ORDER BY CategoryName", connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new { ID = reader.GetInt32(0), Name = reader.GetString(1) });
            }
            return results;
        }

        public async Task<IEnumerable<dynamic>> GetUnitsAsync()
        {
            var results = new List<dynamic>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT UnitID, UnitName, UnitCode FROM Units WHERE IsActive = 1 ORDER BY UnitName", connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new { ID = reader.GetInt32(0), Name = reader.GetString(1), Code = reader.GetString(2) });
            }
            return results;
        }

        public async Task<bool> IsSkuExistsAsync(string sku)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT COUNT(*) FROM Products WHERE SKU = @SKU", connection);
            command.Parameters.AddWithValue("@SKU", sku);
            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            var count = result != null ? (int)result : 0;
            return count > 0;
        }

        public async Task<ProductDto?> GetProductByIdAsync(int productId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT p.*, c.CategoryName, u.UnitCode
                    FROM Products p
                    LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                    LEFT JOIN Units u ON p.UnitID = u.UnitID
                    WHERE p.ProductID = @ProductID", connection);

                command.Parameters.AddWithValue("@ProductID", productId);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new ProductDto
                    {
                        ProductID = reader.GetInt32("ProductID"),
                        SKU = reader.GetString("SKU"),
                        ProductName = reader.GetString("ProductName"),
                        CategoryID = reader.IsDBNull("CategoryID") ? 0 : reader.GetInt32("CategoryID"),
                        CategoryName = reader.IsDBNull("CategoryName") ? "" : reader.GetString("CategoryName"),
                        UnitID = reader.IsDBNull("UnitID") ? 0 : reader.GetInt32("UnitID"),
                        UnitCode = reader.IsDBNull("UnitCode") ? "" : reader.GetString("UnitCode"),
                        ProductType = reader.IsDBNull("ProductType") ? "" : reader.GetString("ProductType"),
                        ReorderLevel = reader.IsDBNull("ReorderLevel") ? 0 : reader.GetDecimal("ReorderLevel"),
                        StandardCost = reader.IsDBNull("StandardCost") ? 0 : reader.GetDecimal("StandardCost"),
                        SellingPrice = reader.IsDBNull("SellingPrice") ? 0 : reader.GetDecimal("SellingPrice"),
                        WeightKg = reader.IsDBNull("WeightKg") ? 0 : reader.GetDecimal("WeightKg"),
                        Dimensions = reader.IsDBNull("Dimensions") ? "" : reader.GetString("Dimensions"),
                        Status = reader.IsDBNull("Status") ? "Active" : reader.GetString("Status"),
                        Description = reader.IsDBNull("Description") ? "" : reader.GetString("Description"),
                        ImageUrl = reader.IsDBNull("ImageUrl") ? "" : reader.GetString("ImageUrl")
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching the product.", ex);
            }
        }

        public async Task<bool> UpdateProductAsync(ProductUpdateDto product)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    UPDATE Products SET 
                        SKU = @SKU, 
                        ProductName = @ProductName, 
                        CategoryID = @CategoryID, 
                        UnitID = @UnitID, 
                        ProductType = @ProductType, 
                        ReorderLevel = @ReorderLevel, 
                        StandardCost = @StandardCost, 
                        SellingPrice = @SellingPrice, 
                        WeightKg = @WeightKg, 
                        Dimensions = @Dimensions, 
                        Status = @Status,
                        Description = @Description,
                        ImageUrl = @ImageUrl,
                        UpdatedAt = GETDATE()
                    WHERE ProductID = @ProductID", connection);

                command.Parameters.AddWithValue("@ProductID", product.ProductID);
                command.Parameters.AddWithValue("@SKU", product.SKU);
                command.Parameters.AddWithValue("@ProductName", product.ProductName);
                command.Parameters.AddWithValue("@CategoryID", product.CategoryID);
                command.Parameters.AddWithValue("@UnitID", product.UnitID);
                command.Parameters.AddWithValue("@ProductType", product.ProductType);
                command.Parameters.AddWithValue("@ReorderLevel", product.ReorderLevel);
                command.Parameters.AddWithValue("@StandardCost", product.StandardCost);
                command.Parameters.AddWithValue("@SellingPrice", product.SellingPrice);
                command.Parameters.AddWithValue("@WeightKg", product.WeightKg);
                command.Parameters.AddWithValue("@Dimensions", product.Dimensions ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Status", product.Status);
                command.Parameters.AddWithValue("@Description", product.Description ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ImageUrl", product.ImageUrl ?? (object)DBNull.Value);

                await connection.OpenAsync();
                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                throw new Exception("Product SKU already exists.");
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while updating the product.", ex);
            }
        }

        public async Task<bool> IsSkuExistsForOtherProductAsync(string sku, int productId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT COUNT(*) FROM Products WHERE SKU = @SKU AND ProductID != @ProductID", connection);
            command.Parameters.AddWithValue("@SKU", sku);
            command.Parameters.AddWithValue("@ProductID", productId);
            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            var count = result != null ? (int)result : 0;
            return count > 0;
        }

        public async Task<bool> DeactivateProductAsync(int productId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    UPDATE Products 
                    SET Status = 'Inactive', UpdatedAt = GETDATE()
                    WHERE ProductID = @ProductID", connection);

                command.Parameters.AddWithValue("@ProductID", productId);

                await connection.OpenAsync();
                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while deactivating the product.", ex);
            }
        }

        public async Task<IEnumerable<dynamic>> GetProductsByLocationAsync(int? warehouseId, int? floorId, int? shelfId)
        {
            var products = new List<dynamic>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                string sql;
                if (warehouseId.HasValue || floorId.HasValue || shelfId.HasValue)
                {
                    sql = @"
                        SELECT DISTINCT p.ProductID, p.ProductName, p.SKU, ISNULL(c.CategoryName, '') AS CategoryName, ISNULL(u.UnitCode, '') AS UnitCode
                        FROM Products p
                        INNER JOIN InventoryStocks s ON p.ProductID = s.ProductID
                        INNER JOIN Shelves sh ON s.ShelfID = sh.ShelfID
                        INNER JOIN Racks r ON sh.RackID = r.RackID
                        INNER JOIN Rooms rm ON r.RoomID = rm.RoomID
                        INNER JOIN Floors f ON rm.FloorID = f.FloorID
                        LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                        LEFT JOIN Units u ON p.UnitID = u.UnitID
                        WHERE 1=1";
                    
                    if (warehouseId.HasValue) sql += " AND f.WarehouseID = @w";
                    if (floorId.HasValue) sql += " AND f.FloorID = @f";
                    if (shelfId.HasValue) sql += " AND sh.ShelfID = @s";

                    sql += " ORDER BY p.ProductName";
                }
                else
                {
                    sql = @"
                        SELECT p.ProductID, p.ProductName, p.SKU, ISNULL(c.CategoryName, '') AS CategoryName, ISNULL(u.UnitCode, '') AS UnitCode
                        FROM Products p
                        LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                        LEFT JOIN Units u ON p.UnitID = u.UnitID
                        WHERE p.Status = 'Active' OR p.Status IS NULL
                        ORDER BY p.ProductName";
                }

                using var command = new SqlCommand(sql, connection);
                if (warehouseId.HasValue) command.Parameters.AddWithValue("@w", warehouseId.Value);
                if (floorId.HasValue) command.Parameters.AddWithValue("@f", floorId.Value);
                if (shelfId.HasValue) command.Parameters.AddWithValue("@s", shelfId.Value);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    products.Add(new {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Sku = reader.GetString(2),
                        Category = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        Unit = reader.IsDBNull(4) ? "" : reader.GetString(4)
                    });
                }
            }
            catch (Exception ex)
            {
                // Return empty if failure or offline, to avoid confusing mock data in production
                Console.WriteLine($"Error fetching products by location: {ex.Message}");
            }
            return products;
        }

        public async Task<IEnumerable<CategoryDetailDto>> GetCategoriesDetailedAsync()
        {
            var categories = new List<CategoryDetailDto>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT c.CategoryID, c.CategoryName, ISNULL(c.Description, '') AS Description, ISNULL(c.IsActive, 1) AS IsActive,
                           COUNT(p.ProductID) AS ProductCount
                    FROM Categories c
                    LEFT JOIN Products p ON c.CategoryID = p.CategoryID
                    GROUP BY c.CategoryID, c.CategoryName, c.Description, c.IsActive
                    ORDER BY c.CategoryName", connection);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    categories.Add(new CategoryDetailDto
                    {
                        CategoryID = reader.GetInt32(0),
                        CategoryName = reader.GetString(1),
                        Description = reader.GetString(2),
                        IsActive = reader.GetBoolean(3),
                        ProductCount = reader.GetInt32(4)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching categories: {ex.Message}");
            }
            return categories;
        }

        public async Task<ProductActionResultDto> SaveCategoryAsync(CategorySaveDto category)
        {
            if (string.IsNullOrWhiteSpace(category.CategoryName))
            {
                return new ProductActionResultDto { Success = false, Message = "Category name is required." };
            }

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                if (category.CategoryID <= 0)
                {
                    // Check duplicate name
                    using var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Categories WHERE LOWER(CategoryName) = LOWER(@Name)", connection);
                    checkCmd.Parameters.AddWithValue("@Name", category.CategoryName.Trim());
                    int count = (int)(await checkCmd.ExecuteScalarAsync() ?? 0);
                    if (count > 0)
                    {
                        return new ProductActionResultDto { Success = false, Message = $"Category '{category.CategoryName}' already exists." };
                    }

                    using var insertCmd = new SqlCommand(@"
                        INSERT INTO Categories (CategoryName, Description, IsActive)
                        VALUES (@Name, @Desc, @IsActive);
                        SELECT SCOPE_IDENTITY();", connection);
                    insertCmd.Parameters.AddWithValue("@Name", category.CategoryName.Trim());
                    insertCmd.Parameters.AddWithValue("@Desc", string.IsNullOrWhiteSpace(category.Description) ? (object)DBNull.Value : category.Description.Trim());
                    insertCmd.Parameters.AddWithValue("@IsActive", category.IsActive);

                    var newId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());
                    return new ProductActionResultDto { Success = true, Message = "Category created successfully.", NewID = newId };
                }
                else
                {
                    // Check duplicate name for other category
                    using var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Categories WHERE LOWER(CategoryName) = LOWER(@Name) AND CategoryID <> @ID", connection);
                    checkCmd.Parameters.AddWithValue("@Name", category.CategoryName.Trim());
                    checkCmd.Parameters.AddWithValue("@ID", category.CategoryID);
                    int count = (int)(await checkCmd.ExecuteScalarAsync() ?? 0);
                    if (count > 0)
                    {
                        return new ProductActionResultDto { Success = false, Message = $"Another category with name '{category.CategoryName}' already exists." };
                    }

                    // If turning inactive, check active products
                    if (!category.IsActive)
                    {
                        using var prodCheckCmd = new SqlCommand("SELECT COUNT(*) FROM Products WHERE CategoryID = @ID AND (Status = 'Active' OR Status IS NULL)", connection);
                        prodCheckCmd.Parameters.AddWithValue("@ID", category.CategoryID);
                        int activeProds = (int)(await prodCheckCmd.ExecuteScalarAsync() ?? 0);
                        if (activeProds > 0)
                        {
                            return new ProductActionResultDto { Success = false, Message = $"Cannot set category to inactive because {activeProds} active product(s) are assigned to it." };
                        }
                    }

                    using var updateCmd = new SqlCommand(@"
                        UPDATE Categories 
                        SET CategoryName = @Name, Description = @Desc, IsActive = @IsActive
                        WHERE CategoryID = @ID", connection);
                    updateCmd.Parameters.AddWithValue("@Name", category.CategoryName.Trim());
                    updateCmd.Parameters.AddWithValue("@Desc", string.IsNullOrWhiteSpace(category.Description) ? (object)DBNull.Value : category.Description.Trim());
                    updateCmd.Parameters.AddWithValue("@IsActive", category.IsActive);
                    updateCmd.Parameters.AddWithValue("@ID", category.CategoryID);

                    await updateCmd.ExecuteNonQueryAsync();
                    return new ProductActionResultDto { Success = true, Message = "Category updated successfully." };
                }
            }
            catch (Exception ex)
            {
                return new ProductActionResultDto { Success = false, Message = $"Database error: {ex.Message}" };
            }
        }

        public async Task<ProductActionResultDto> ToggleCategoryStatusAsync(int categoryId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Get current status & name
                using var getCmd = new SqlCommand("SELECT CategoryName, ISNULL(IsActive, 1) FROM Categories WHERE CategoryID = @ID", connection);
                getCmd.Parameters.AddWithValue("@ID", categoryId);
                using var reader = await getCmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    return new ProductActionResultDto { Success = false, Message = "Category not found." };
                }

                string name = reader.GetString(0);
                bool isActive = reader.GetBoolean(1);
                reader.Close();

                if (isActive)
                {
                    // Check if products actively use this category
                    using var countCmd = new SqlCommand("SELECT COUNT(*) FROM Products WHERE CategoryID = @ID AND (Status = 'Active' OR Status IS NULL)", connection);
                    countCmd.Parameters.AddWithValue("@ID", categoryId);
                    int activeProds = (int)(await countCmd.ExecuteScalarAsync() ?? 0);
                    if (activeProds > 0)
                    {
                        return new ProductActionResultDto { Success = false, Message = $"Cannot deactivate category '{name}' because {activeProds} active product(s) are assigned to it." };
                    }
                }

                using var toggleCmd = new SqlCommand("UPDATE Categories SET IsActive = CASE WHEN ISNULL(IsActive, 1) = 1 THEN 0 ELSE 1 END WHERE CategoryID = @ID", connection);
                toggleCmd.Parameters.AddWithValue("@ID", categoryId);
                await toggleCmd.ExecuteNonQueryAsync();

                return new ProductActionResultDto { Success = true, Message = $"Category '{name}' status updated to {(isActive ? "Inactive" : "Active")}." };
            }
            catch (Exception ex)
            {
                return new ProductActionResultDto { Success = false, Message = $"Database error: {ex.Message}" };
            }
        }

        public async Task<IEnumerable<UnitDetailDto>> GetUnitsDetailedAsync()
        {
            var units = new List<UnitDetailDto>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT u.UnitID, u.UnitName, u.UnitCode, ISNULL(u.IsActive, 1) AS IsActive,
                           COUNT(p.ProductID) AS ProductCount
                    FROM Units u
                    LEFT JOIN Products p ON u.UnitID = p.UnitID
                    GROUP BY u.UnitID, u.UnitName, u.UnitCode, u.IsActive
                    ORDER BY u.UnitName", connection);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    units.Add(new UnitDetailDto
                    {
                        UnitID = reader.GetInt32(0),
                        UnitName = reader.GetString(1),
                        UnitCode = reader.GetString(2),
                        IsActive = reader.GetBoolean(3),
                        ProductCount = reader.GetInt32(4)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching units: {ex.Message}");
            }
            return units;
        }

        public async Task<ProductActionResultDto> SaveUnitAsync(UnitSaveDto unit)
        {
            if (string.IsNullOrWhiteSpace(unit.UnitName))
            {
                return new ProductActionResultDto { Success = false, Message = "Unit name is required." };
            }
            if (string.IsNullOrWhiteSpace(unit.UnitCode))
            {
                return new ProductActionResultDto { Success = false, Message = "Unit code (abbreviation) is required." };
            }

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                if (unit.UnitID <= 0)
                {
                    // Check duplicate name or code
                    using var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Units WHERE LOWER(UnitName) = LOWER(@Name) OR LOWER(UnitCode) = LOWER(@Code)", connection);
                    checkCmd.Parameters.AddWithValue("@Name", unit.UnitName.Trim());
                    checkCmd.Parameters.AddWithValue("@Code", unit.UnitCode.Trim());
                    int count = (int)(await checkCmd.ExecuteScalarAsync() ?? 0);
                    if (count > 0)
                    {
                        return new ProductActionResultDto { Success = false, Message = $"Unit with name '{unit.UnitName}' or code '{unit.UnitCode}' already exists." };
                    }

                    using var insertCmd = new SqlCommand(@"
                        INSERT INTO Units (UnitName, UnitCode, IsActive)
                        VALUES (@Name, @Code, @IsActive);
                        SELECT SCOPE_IDENTITY();", connection);
                    insertCmd.Parameters.AddWithValue("@Name", unit.UnitName.Trim());
                    insertCmd.Parameters.AddWithValue("@Code", unit.UnitCode.Trim().ToUpper());
                    insertCmd.Parameters.AddWithValue("@IsActive", unit.IsActive);

                    var newId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());
                    return new ProductActionResultDto { Success = true, Message = "Unit created successfully.", NewID = newId };
                }
                else
                {
                    // Check duplicate name or code for other unit
                    using var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Units WHERE (LOWER(UnitName) = LOWER(@Name) OR LOWER(UnitCode) = LOWER(@Code)) AND UnitID <> @ID", connection);
                    checkCmd.Parameters.AddWithValue("@Name", unit.UnitName.Trim());
                    checkCmd.Parameters.AddWithValue("@Code", unit.UnitCode.Trim());
                    checkCmd.Parameters.AddWithValue("@ID", unit.UnitID);
                    int count = (int)(await checkCmd.ExecuteScalarAsync() ?? 0);
                    if (count > 0)
                    {
                        return new ProductActionResultDto { Success = false, Message = $"Another unit with name '{unit.UnitName}' or code '{unit.UnitCode}' already exists." };
                    }

                    // If turning inactive, check active products
                    if (!unit.IsActive)
                    {
                        using var prodCheckCmd = new SqlCommand("SELECT COUNT(*) FROM Products WHERE UnitID = @ID AND (Status = 'Active' OR Status IS NULL)", connection);
                        prodCheckCmd.Parameters.AddWithValue("@ID", unit.UnitID);
                        int activeProds = (int)(await prodCheckCmd.ExecuteScalarAsync() ?? 0);
                        if (activeProds > 0)
                        {
                            return new ProductActionResultDto { Success = false, Message = $"Cannot set unit to inactive because {activeProds} active product(s) are using it." };
                        }
                    }

                    using var updateCmd = new SqlCommand(@"
                        UPDATE Units 
                        SET UnitName = @Name, UnitCode = @Code, IsActive = @IsActive
                        WHERE UnitID = @ID", connection);
                    updateCmd.Parameters.AddWithValue("@Name", unit.UnitName.Trim());
                    updateCmd.Parameters.AddWithValue("@Code", unit.UnitCode.Trim().ToUpper());
                    updateCmd.Parameters.AddWithValue("@IsActive", unit.IsActive);
                    updateCmd.Parameters.AddWithValue("@ID", unit.UnitID);

                    await updateCmd.ExecuteNonQueryAsync();
                    return new ProductActionResultDto { Success = true, Message = "Unit updated successfully." };
                }
            }
            catch (Exception ex)
            {
                return new ProductActionResultDto { Success = false, Message = $"Database error: {ex.Message}" };
            }
        }

        public async Task<ProductActionResultDto> ToggleUnitStatusAsync(int unitId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Get current status & name
                using var getCmd = new SqlCommand("SELECT UnitName, ISNULL(IsActive, 1) FROM Units WHERE UnitID = @ID", connection);
                getCmd.Parameters.AddWithValue("@ID", unitId);
                using var reader = await getCmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    return new ProductActionResultDto { Success = false, Message = "Unit not found." };
                }

                string name = reader.GetString(0);
                bool isActive = reader.GetBoolean(1);
                reader.Close();

                if (isActive)
                {
                    // Check if products actively use this unit
                    using var countCmd = new SqlCommand("SELECT COUNT(*) FROM Products WHERE UnitID = @ID AND (Status = 'Active' OR Status IS NULL)", connection);
                    countCmd.Parameters.AddWithValue("@ID", unitId);
                    int activeProds = (int)(await countCmd.ExecuteScalarAsync() ?? 0);
                    if (activeProds > 0)
                    {
                        return new ProductActionResultDto { Success = false, Message = $"Cannot deactivate unit '{name}' because {activeProds} active product(s) are using it." };
                    }
                }

                using var toggleCmd = new SqlCommand("UPDATE Units SET IsActive = CASE WHEN ISNULL(IsActive, 1) = 1 THEN 0 ELSE 1 END WHERE UnitID = @ID", connection);
                toggleCmd.Parameters.AddWithValue("@ID", unitId);
                await toggleCmd.ExecuteNonQueryAsync();

                return new ProductActionResultDto { Success = true, Message = $"Unit '{name}' status updated to {(isActive ? "Inactive" : "Active")}." };
            }
            catch (Exception ex)
            {
                return new ProductActionResultDto { Success = false, Message = $"Database error: {ex.Message}" };
            }
        }
    }
}

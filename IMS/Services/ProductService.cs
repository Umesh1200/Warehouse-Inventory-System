using IMS.Models.DTOs;
using IMS.Repositories;

namespace IMS.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;

        public ProductService(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<IEnumerable<ProductDto>> GetProductMasterAsync()
        {
            var products = await _productRepository.GetAllProductsAsync();
            
            // Note: In a real app, low stock might be calculated against live inventory levels
            // For now, we use a placeholder logic or keep the IsLowStock flag as false
            return products;
        }

        public async Task<ProductSummaryDto> GetProductSummaryAsync()
        {
            var products = await _productRepository.GetAllProductsAsync();
            
            return new ProductSummaryDto
            {
                TotalProducts = products.Count(),
                RawMaterials = products.Count(p => p.ProductType == "Raw Material"),
                FinishedGoods = products.Count(p => p.ProductType == "Finished Goods"),
                LowStockSkus = 0 // Placeholder until inventory integration
            };
        }

        public async Task<ProductActionResultDto> CreateProductAsync(ProductCreateDto product)
        {
            // 1. Validation
            if (string.IsNullOrWhiteSpace(product.SKU)) return new ProductActionResultDto { Success = false, Message = "SKU is required." };
            if (string.IsNullOrWhiteSpace(product.ProductName)) return new ProductActionResultDto { Success = false, Message = "Product Name is required." };
            if (product.CategoryID <= 0) return new ProductActionResultDto { Success = false, Message = "Invalid Category." };
            if (product.UnitID <= 0) return new ProductActionResultDto { Success = false, Message = "Invalid Unit." };
            if (string.IsNullOrWhiteSpace(product.ProductType)) return new ProductActionResultDto { Success = false, Message = "Product Type is required." };
            
            if (product.ReorderLevel < 0) return new ProductActionResultDto { Success = false, Message = "Reorder Level cannot be negative." };
            if (product.StandardCost < 0) return new ProductActionResultDto { Success = false, Message = "Standard Cost cannot be negative." };
            if (product.SellingPrice < 0) return new ProductActionResultDto { Success = false, Message = "Selling Price cannot be negative." };
            if (product.WeightKg < 0) return new ProductActionResultDto { Success = false, Message = "Weight cannot be negative." };

            // 2. Check duplicate SKU
            if (await _productRepository.IsSkuExistsAsync(product.SKU))
            {
                return new ProductActionResultDto { Success = false, Message = $"Product with SKU '{product.SKU}' already exists." };
            }

            // 3. Create
            try
            {
                int newId = await _productRepository.CreateProductAsync(product);
                return new ProductActionResultDto { Success = true, Message = "Product created successfully.", NewID = newId };
            }
            catch (Exception ex)
            {
                return new ProductActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<ProductActionResultDto> UpdateProductAsync(ProductUpdateDto product)
        {
            if (product.ProductID <= 0) return new ProductActionResultDto { Success = false, Message = "Invalid Product ID." };
            if (string.IsNullOrWhiteSpace(product.SKU)) return new ProductActionResultDto { Success = false, Message = "SKU is required." };
            if (string.IsNullOrWhiteSpace(product.ProductName)) return new ProductActionResultDto { Success = false, Message = "Product Name is required." };
            if (product.CategoryID <= 0) return new ProductActionResultDto { Success = false, Message = "Invalid Category." };
            if (product.UnitID <= 0) return new ProductActionResultDto { Success = false, Message = "Invalid Unit." };
            if (string.IsNullOrWhiteSpace(product.ProductType)) return new ProductActionResultDto { Success = false, Message = "Product Type is required." };
            
            if (product.ReorderLevel < 0) return new ProductActionResultDto { Success = false, Message = "Reorder Level cannot be negative." };
            if (product.StandardCost < 0) return new ProductActionResultDto { Success = false, Message = "Standard Cost cannot be negative." };
            if (product.SellingPrice < 0) return new ProductActionResultDto { Success = false, Message = "Selling Price cannot be negative." };
            if (product.WeightKg < 0) return new ProductActionResultDto { Success = false, Message = "Weight cannot be negative." };

            if (await _productRepository.IsSkuExistsForOtherProductAsync(product.SKU, product.ProductID))
            {
                return new ProductActionResultDto { Success = false, Message = $"Product with SKU '{product.SKU}' already exists." };
            }

            try
            {
                bool success = await _productRepository.UpdateProductAsync(product);
                if (success)
                    return new ProductActionResultDto { Success = true, Message = "Product updated successfully." };
                else
                    return new ProductActionResultDto { Success = false, Message = "Product not found or no changes made." };
            }
            catch (Exception ex)
            {
                return new ProductActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<ProductActionResultDto> DeactivateProductAsync(int productId)
        {
            if (productId <= 0) return new ProductActionResultDto { Success = false, Message = "Invalid Product ID." };

            try
            {
                bool success = await _productRepository.DeactivateProductAsync(productId);
                if (success)
                    return new ProductActionResultDto { Success = true, Message = "Product deactivated. Existing history is preserved." };
                else
                    return new ProductActionResultDto { Success = false, Message = "Product not found or already inactive." };
            }
            catch (Exception ex)
            {
                return new ProductActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<ProductDto?> GetProductByIdAsync(int productId)
        {
            return await _productRepository.GetProductByIdAsync(productId);
        }

        public async Task<IEnumerable<dynamic>> GetCategoriesAsync()
        {
            return await _productRepository.GetCategoriesAsync();
        }

        public async Task<IEnumerable<dynamic>> GetUnitsAsync()
        {
            return await _productRepository.GetUnitsAsync();
        }

        public async Task<ProductInsightsDto> GetProductInsightsAsync(int productId)
        {
            // Fetch actual product to know standard cost/price if we want realistic mock
            var product = await _productRepository.GetProductByIdAsync(productId);
            var stdCost = product?.StandardCost ?? 1000m;
            var sellingPrice = product?.SellingPrice ?? 1200m;

            var insights = new ProductInsightsDto();
            var random = new Random(productId); // predictable random
            
            decimal currentStock = random.Next(100, 500);

            // Generate 30 days of mock data
            for (int i = 29; i >= 0; i--)
            {
                var date = DateTime.Today.AddDays(-i);
                insights.Labels.Add(date.ToString("MMM dd"));
                
                decimal dailyIn = random.Next(0, 10) > 7 ? random.Next(10, 50) : 0;
                decimal dailyOut = random.Next(0, 10) > 4 ? random.Next(5, 20) : 0;
                
                // Purchase/Sales volume
                decimal purchaseVol = dailyIn * stdCost;
                decimal salesVol = dailyOut * sellingPrice;

                currentStock = currentStock + dailyIn - dailyOut;
                if (currentStock < 0) currentStock = 0;

                insights.StockLevels.Add(currentStock);
                insights.StockIn.Add(dailyIn);
                insights.StockOut.Add(dailyOut);
                insights.PurchaseVolume.Add(purchaseVol);
                insights.SalesVolume.Add(salesVol);
                insights.Valuation.Add(currentStock * stdCost);
            }

            return insights;
        }

        public async Task<IEnumerable<dynamic>> GetProductsByLocationAsync(int? warehouseId, int? floorId, int? shelfId)
        {
            return await _productRepository.GetProductsByLocationAsync(warehouseId, floorId, shelfId);
        }

        public async Task<IEnumerable<CategoryDetailDto>> GetCategoriesDetailedAsync()
        {
            return await _productRepository.GetCategoriesDetailedAsync();
        }

        public async Task<ProductActionResultDto> SaveCategoryAsync(CategorySaveDto category)
        {
            return await _productRepository.SaveCategoryAsync(category);
        }

        public async Task<ProductActionResultDto> ToggleCategoryStatusAsync(int categoryId)
        {
            return await _productRepository.ToggleCategoryStatusAsync(categoryId);
        }

        public async Task<IEnumerable<UnitDetailDto>> GetUnitsDetailedAsync()
        {
            return await _productRepository.GetUnitsDetailedAsync();
        }

        public async Task<ProductActionResultDto> SaveUnitAsync(UnitSaveDto unit)
        {
            return await _productRepository.SaveUnitAsync(unit);
        }

        public async Task<ProductActionResultDto> ToggleUnitStatusAsync(int unitId)
        {
            return await _productRepository.ToggleUnitStatusAsync(unitId);
        }
    }
}

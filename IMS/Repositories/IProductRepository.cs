using IMS.Models.DTOs;

namespace IMS.Repositories
{
    public interface IProductRepository
    {
        Task<IEnumerable<ProductDto>> GetAllProductsAsync();
        Task<int> CreateProductAsync(ProductCreateDto product);
        Task<IEnumerable<dynamic>> GetCategoriesAsync();
        Task<IEnumerable<dynamic>> GetUnitsAsync();
        Task<bool> IsSkuExistsAsync(string sku);
        Task<IEnumerable<dynamic>> GetProductsByLocationAsync(int? warehouseId, int? floorId, int? shelfId);
        Task<ProductDto?> GetProductByIdAsync(int productId);
        Task<bool> UpdateProductAsync(ProductUpdateDto product);
        Task<bool> IsSkuExistsForOtherProductAsync(string sku, int productId);
        Task<bool> DeactivateProductAsync(int productId);
        
        // Category Management
        Task<IEnumerable<CategoryDetailDto>> GetCategoriesDetailedAsync();
        Task<ProductActionResultDto> SaveCategoryAsync(CategorySaveDto category);
        Task<ProductActionResultDto> ToggleCategoryStatusAsync(int categoryId);

        // Unit Management
        Task<IEnumerable<UnitDetailDto>> GetUnitsDetailedAsync();
        Task<ProductActionResultDto> SaveUnitAsync(UnitSaveDto unit);
        Task<ProductActionResultDto> ToggleUnitStatusAsync(int unitId);
    }
}

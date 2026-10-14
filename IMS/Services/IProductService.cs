using IMS.Models.DTOs;

namespace IMS.Services
{
    public interface IProductService
    {
        Task<IEnumerable<ProductDto>> GetProductMasterAsync();
        Task<ProductSummaryDto> GetProductSummaryAsync();
        Task<ProductActionResultDto> CreateProductAsync(ProductCreateDto product);
        Task<ProductActionResultDto> UpdateProductAsync(ProductUpdateDto product);
        Task<ProductActionResultDto> DeactivateProductAsync(int productId);
        Task<ProductDto?> GetProductByIdAsync(int productId);
        Task<IEnumerable<dynamic>> GetCategoriesAsync();
        Task<IEnumerable<dynamic>> GetUnitsAsync();
        Task<ProductInsightsDto> GetProductInsightsAsync(int productId);
        Task<IEnumerable<dynamic>> GetProductsByLocationAsync(int? warehouseId, int? floorId, int? shelfId);

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

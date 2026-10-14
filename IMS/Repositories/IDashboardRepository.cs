using IMS.Models.DTOs;

namespace IMS.Repositories
{
    public interface IDashboardRepository
    {
        Task<DashboardSummaryDto> GetDashboardSummaryAsync(int? warehouseId = null);
        Task<IEnumerable<DashboardLowStockDto>> GetLowStockItemsAsync(int limit = 10, int? warehouseId = null);
        Task<IEnumerable<DashboardShelfUtilizationDto>> GetShelfUtilizationAsync(int limit = 10, int? warehouseId = null);
        Task<IEnumerable<DashboardMovementDto>> GetRecentMovementsAsync(int limit = 10, int? warehouseId = null);
        Task<IEnumerable<DashboardWarehouseOptionDto>> GetWarehousesAsync();
        Task<IEnumerable<DashboardStockValueDto>> GetStockValueBreakdownAsync(int limit = 8, int? warehouseId = null);
        Task<IEnumerable<DashboardStockValueDto>> GetStockValueByCategoryAsync(int? warehouseId = null);
        Task<IEnumerable<DashboardMovementTrendDto>> GetMovementTrendAsync(int days = 7, int? warehouseId = null);
        Task<IEnumerable<DashboardTopProductDto>> GetTopProductsByValueAsync(int limit = 6, int? warehouseId = null);
        Task<IEnumerable<DashboardTopProductDto>> GetTopSellingProductsAsync(int limit = 6);
        Task<DashboardProcurementFinanceDto> GetProcurementFinanceSummaryAsync();
        Task<IEnumerable<DashboardTopSupplierDto>> GetTopSuppliersAsync(int limit = 5);
        Task<IEnumerable<DashboardPendingPipelineDto>> GetInboundPipelineAsync(int limit = 5);
        Task<IEnumerable<DashboardStaffPerformanceDto>> GetStaffPerformanceAsync(int limit = 6);
    }
}

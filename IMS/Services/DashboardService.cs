using IMS.Models.DTOs;
using IMS.Repositories;

namespace IMS.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _dashboardRepository;

        public DashboardService(IDashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository;
        }

        public async Task<AdminDashboardDataDto> GetAdminDashboardDataAsync(int? warehouseId = null)
        {
            var data = new AdminDashboardDataDto();

            try
            {
                data.Warehouses = (await _dashboardRepository.GetWarehousesAsync()).ToList();
                data.Summary = await _dashboardRepository.GetDashboardSummaryAsync(warehouseId);
                data.LowStockItems = (await _dashboardRepository.GetLowStockItemsAsync(100, warehouseId)).ToList();
                data.ShelfUtilization = (await _dashboardRepository.GetShelfUtilizationAsync(10, warehouseId)).ToList();
                data.RecentMovements = (await _dashboardRepository.GetRecentMovementsAsync(30, warehouseId)).ToList();
                data.StockValueBreakdown = (await _dashboardRepository.GetStockValueBreakdownAsync(9, warehouseId)).ToList();
                data.StockValueByCategory = (await _dashboardRepository.GetStockValueByCategoryAsync(warehouseId)).ToList();
                data.MovementTrend = (await _dashboardRepository.GetMovementTrendAsync(7, warehouseId)).ToList();

                // Executive Hub Data
                data.TopProductsByValue = (await _dashboardRepository.GetTopProductsByValueAsync(6, warehouseId)).ToList();
                data.TopSellingProducts = (await _dashboardRepository.GetTopSellingProductsAsync(6)).ToList();
                data.FinanceSummary = await _dashboardRepository.GetProcurementFinanceSummaryAsync();
                data.TopSuppliers = (await _dashboardRepository.GetTopSuppliersAsync(5)).ToList();
                data.InboundPipeline = (await _dashboardRepository.GetInboundPipelineAsync(5)).ToList();
                data.StaffPerformance = (await _dashboardRepository.GetStaffPerformanceAsync(6)).ToList();
            }
            catch (Exception ex)
            {
                // In a real app, log the exception
                throw new Exception("Error compiling dashboard data.", ex);
            }

            return data;
        }
    }
}

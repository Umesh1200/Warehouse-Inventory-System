using IMS.Models.DTOs;

namespace IMS.Services
{
    public interface IDashboardService
    {
        Task<AdminDashboardDataDto> GetAdminDashboardDataAsync(int? warehouseId = null);
    }
}

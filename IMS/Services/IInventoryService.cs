using IMS.Models.DTOs;

namespace IMS.Services
{
    public interface IInventoryService
    {
        Task<(IEnumerable<InventoryOverviewDto> Items, int TotalCount)> GetOverviewAsync(string? warehouse, string? category, string? status, int pageNumber, int pageSize);
        Task<InventorySummaryDto> GetOverviewSummaryAsync();
    }
}

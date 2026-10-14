using IMS.Models.DTOs;

namespace IMS.Repositories
{
    public interface IInventoryRepository
    {
        Task<(IEnumerable<InventoryOverviewDto> Items, int TotalCount)> GetInventoryOverviewAsync(string? warehouse, string? category, string? status, int pageNumber, int pageSize);
    }
}

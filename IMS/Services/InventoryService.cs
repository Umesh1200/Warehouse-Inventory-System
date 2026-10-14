using IMS.Models.DTOs;
using IMS.Repositories;

namespace IMS.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IInventoryRepository _inventoryRepository;

        public InventoryService(IInventoryRepository inventoryRepository)
        {
            _inventoryRepository = inventoryRepository;
        }

        public async Task<(IEnumerable<InventoryOverviewDto> Items, int TotalCount)> GetOverviewAsync(string? warehouse, string? category, string? status, int pageNumber, int pageSize)
        {
            return await _inventoryRepository.GetInventoryOverviewAsync(warehouse, category, status, pageNumber, pageSize);
        }

        public async Task<InventorySummaryDto> GetOverviewSummaryAsync()
        {
            var (data, totalCount) = await _inventoryRepository.GetInventoryOverviewAsync(null, null, null, 1, int.MaxValue);

            return new InventorySummaryDto
            {
                TotalStockValue = data.Sum(x => x.TotalValue),
                LowStockCount = data.Count(x => x.StockStatus == "Low Stock" || x.StockStatus == "Out of Stock"),
                TotalQCHold = data.Sum(x => x.QCHoldQty),
                TotalDamaged = data.Sum(x => x.DamagedQty)
            };
        }
    }
}

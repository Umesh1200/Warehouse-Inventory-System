using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public interface IInventoryAdjustmentRepository
    {
        Task<IEnumerable<InventoryAdjustmentDto>> GetAllAdjustmentsAsync();
        Task<IEnumerable<InventoryAdjustmentRowDto>> GetAllAdjustmentRowsAsync();
        Task<IEnumerable<InventoryAdjustmentItemDto>> GetAdjustmentItemsAsync(int adjustmentId);
        Task<int> CreateAdjustmentWithItemsAsync(AdjustmentCreateDto adjustment);
        Task<InventoryAdjustmentDto?> GetAdjustmentByIdAsync(int adjustmentId);
        Task<bool> ApproveAdjustmentAsync(int adjustmentId, int approvedBy);
        Task<AdjustmentActionResultDto> PostAdjustmentAsync(int adjustmentId, int performedBy);
        Task<decimal> GetCurrentStockAsync(int productId, int shelfId);
        Task<IEnumerable<dynamic>> GetActiveProductsAsync();
        Task<IEnumerable<dynamic>> GetActiveShelvesAsync();
        Task<IEnumerable<dynamic>> GetShelvesWithProductAsync(int productId);
        Task<IEnumerable<string>> GetWarehousesAsync();
    }
}

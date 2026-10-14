using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Services
{
    public interface IInventoryAdjustmentService
    {
        Task<IEnumerable<InventoryAdjustmentDto>> GetAdjustmentsAsync();
        Task<IEnumerable<InventoryAdjustmentRowDto>> GetAllAdjustmentRowsAsync();
        Task<InventoryAdjustmentDto?> GetAdjustmentByIdAsync(int adjustmentId);
        Task<IEnumerable<InventoryAdjustmentItemDto>> GetAdjustmentItemsAsync(int adjustmentId);
        Task<InventoryAdjustmentSummaryDto> GetAdjustmentSummaryAsync();
        Task<AdjustmentActionResultDto> CreateAdjustmentAsync(AdjustmentCreateDto adjustment);
        Task<AdjustmentActionResultDto> PostAdjustmentAsync(int adjustmentId, int performedBy);
        Task<decimal> GetCurrentStockAsync(int productId, int shelfId);
        Task<AdjustmentActionResultDto> ApproveAdjustmentAsync(int adjustmentId, int approvedBy);
        Task<IEnumerable<dynamic>> GetActiveProductsAsync();
        Task<IEnumerable<dynamic>> GetActiveShelvesAsync();
        Task<IEnumerable<dynamic>> GetShelvesWithProductAsync(int productId);
        Task<IEnumerable<string>> GetWarehousesAsync();
    }
}

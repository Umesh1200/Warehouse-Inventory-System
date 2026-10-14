using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public interface IStockTransferRepository
    {
        Task<IEnumerable<StockTransferDto>> GetAllTransfersAsync();
        Task<IEnumerable<StockTransferItemDto>> GetTransferItemsAsync(int transferId);
        Task<int> CreateTransferWithItemsAsync(StockTransferCreateDto transfer);
        Task<StockTransferDto?> GetTransferByIdAsync(int transferId);
        Task<bool> ApproveTransferAsync(int transferId, int approvedBy);
        Task<bool> CancelTransferAsync(int transferId, int cancelledBy);
        Task<StockTransferActionResultDto> PostTransferAsync(int transferId, int performedBy);
        Task<decimal> GetAvailableStockAsync(int productId, int shelfId);
        Task<decimal> GetShelfAvailableCapacityAsync(int shelfId);
        Task<decimal> GetShelfAvailableWeightCapacityAsync(int shelfId);
        Task<decimal> GetRequiredWeightAsync(int productId, decimal quantity);
        Task<bool> IsShelfCompatibleForProductAsync(int productId, int shelfId);
        Task<string> DeriveTransferTypeAsync(int fromShelfId, int toShelfId);
        Task<string> GetNextTransferNoAsync();
        Task<IEnumerable<dynamic>> GetActiveProductsAsync();
        Task<IEnumerable<dynamic>> GetActiveShelvesAsync();
        Task<IEnumerable<dynamic>> GetShelvesWithProductAsync(int productId);
        Task<IEnumerable<dynamic>> GetProductsByShelfAsync(int shelfId);
        Task<IEnumerable<ShelfOptionDto>> GetDestinationShelvesForProductAsync(int productId, decimal quantity);
        Task<ShelfCapacityInfoDto?> GetShelfCapacityInfoAsync(int shelfId, int? productId = null);
    }
}

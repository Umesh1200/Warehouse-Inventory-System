using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Services
{
    public interface IStockTransferService
    {
        Task<IEnumerable<StockTransferDto>> GetTransfersAsync();
        Task<IEnumerable<StockTransferItemDto>> GetTransferItemsAsync(int transferId);
        Task<StockTransferSummaryDto> GetTransferSummaryAsync();
        Task<StockTransferActionResultDto> CreateTransferAsync(StockTransferCreateDto transfer);
        Task<StockTransferActionResultDto> ApproveTransferAsync(int transferId, int approvedBy);
        Task<StockTransferActionResultDto> CancelTransferAsync(int transferId, int cancelledBy);
        Task<StockTransferActionResultDto> PostTransferAsync(int transferId, int performedBy);
        Task<IEnumerable<dynamic>> GetActiveProductsAsync();
        Task<decimal> GetShelfAvailableWeightCapacityAsync(int shelfId);
        Task<IEnumerable<dynamic>> GetActiveShelvesAsync();
        Task<IEnumerable<dynamic>> GetShelvesWithProductAsync(int productId);
        Task<IEnumerable<dynamic>> GetProductsByShelfAsync(int shelfId);
        Task<IEnumerable<ShelfOptionDto>> GetDestinationShelvesForProductAsync(int productId, decimal quantity);
        Task<ShelfCapacityInfoDto?> GetShelfCapacityInfoAsync(int shelfId, int? productId = null);
    }
}

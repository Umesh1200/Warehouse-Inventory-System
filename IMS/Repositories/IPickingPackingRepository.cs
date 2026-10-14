using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public interface IPickingPackingRepository
    {
        Task<IEnumerable<PickingPackingDto>> GetPickPackTasksAsync();
        Task<IEnumerable<PickingPackingItemDto>> GetTaskItemsAsync(int soid);
        Task<PickingPackingActionResultDto> CreatePickTaskAsync(CreatePickTaskDto task);
        Task<PickingPackingActionResultDto> StartPickingAsync(int pickId, int userId);
        Task<PickingPackingActionResultDto> MarkPickedAsync(int pickId, int userId);
        Task<PickingPackingActionResultDto> CompletePackingAsync(PackingCreateDto packing);
        Task<SalesOrderForPickingDto?> GetSalesOrderForPickingAsync(int soId);
        Task<decimal> GetAvailableStockAsync(int productId, int shelfId);
        Task<IEnumerable<dynamic>> GetConfirmedSalesOrdersAsync();
        Task<IEnumerable<dynamic>> GetActiveWarehouseStaffAsync();
        Task<IEnumerable<dynamic>> GetActiveShelvesAsync();
        Task<IEnumerable<dynamic>> GetShelvesWithProductAsync(int productId);
    }
}

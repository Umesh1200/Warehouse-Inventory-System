using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Services
{
    public interface IPickingPackingService
    {
        Task<IEnumerable<PickingPackingDto>> GetPickPackTasksAsync();
        Task<IEnumerable<PickingPackingItemDto>> GetTaskItemsAsync(int soid);
        Task<PickingPackingSummaryDto> GetPickPackSummaryAsync();
        Task<PickingPackingActionResultDto> CreatePickTaskAsync(CreatePickTaskDto task);
        Task<PickingPackingActionResultDto> StartPickingAsync(int pickId, int userId);
        Task<PickingPackingActionResultDto> MarkPickedAsync(int pickId, int userId);
        Task<PickingPackingActionResultDto> CompletePackingAsync(PackingCreateDto packing);
        Task<SalesOrderForPickingDto?> GetSalesOrderForPickingAsync(int soId);
        Task<IEnumerable<dynamic>> GetConfirmedSalesOrdersAsync();
        Task<IEnumerable<dynamic>> GetActiveWarehouseStaffAsync();
        Task<IEnumerable<dynamic>> GetActiveShelvesAsync();
        Task<IEnumerable<dynamic>> GetShelvesWithProductAsync(int productId);
    }
}

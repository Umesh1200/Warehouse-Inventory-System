using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public interface IDispatchRepository
    {
        Task<IEnumerable<DispatchTrackingDto>> GetDispatchTasksAsync();
        Task<IEnumerable<DispatchItemDto>> GetDispatchItemsAsync(int soid);
        Task<DispatchActionResultDto> CreateDispatchAsync(DispatchCreateDto dispatch);
        Task<DispatchActionResultDto> ConfirmDispatchAsync(int dispatchId, int performedBy);
        Task<DispatchActionResultDto> MarkDeliveredAsync(int dispatchId);
        Task<DispatchActionResultDto> MarkFailedAsync(int dispatchId);
        Task<DispatchTrackingDto?> GetDispatchByIdAsync(int dispatchId);
        Task<SalesOrderForDispatchDto?> GetSalesOrderForDispatchAsync(int soId);
        Task<bool> HasDispatchForSOAsync(int soId);
    }
}

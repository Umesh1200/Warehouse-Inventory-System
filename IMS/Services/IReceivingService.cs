using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Services
{
    public interface IReceivingService
    {
        Task<IEnumerable<ReceivingDto>> GetReceivingTasksAsync();
        Task<ReceivingSummaryDto> GetReceivingSummaryAsync();
        Task<ReceivingActionResultDto> ReceiveItemAsync(ReceivingCreateDto receipt);
        Task<PurchaseOrderItemReceivingDto?> GetPOItemForReceivingAsync(int poItemId);
    }
}

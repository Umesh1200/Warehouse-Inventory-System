using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public interface IReceivingRepository
    {
        Task<IEnumerable<ReceivingDto>> GetReceivingTasksAsync();
        Task<int> CreateReceiptAsync(ReceivingCreateDto receipt);
        Task<PurchaseOrderItemReceivingDto?> GetPOItemForReceivingAsync(int poItemId);
        Task<decimal> GetRemainingQtyAsync(int poItemId);
    }
}

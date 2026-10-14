using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public interface IFinishedGoodsReceiptRepository
    {
        Task<IEnumerable<FinishedGoodsReceiptDto>> GetAllReceiptsAsync();
        Task<int> CreateFinishedGoodsReceiptAsync(FinishedGoodsReceiptCreateDto receipt);
        Task<bool> ReceiveFinishedGoodsAsync(int receiptId, int performedBy);
        Task<ProductionPlanForReceiptDto?> GetProductionPlanForReceiptAsync(int planId);
        Task<bool> IsReceiptNumberExistsAsync(string receiptNumber);
        Task<IEnumerable<dynamic>> GetActiveProductionPlansAsync();
        Task<IEnumerable<dynamic>> GetActiveStaffAsync();
    }
}

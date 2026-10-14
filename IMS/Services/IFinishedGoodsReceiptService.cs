using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Services
{
    public interface IFinishedGoodsReceiptService
    {
        Task<IEnumerable<FinishedGoodsReceiptDto>> GetReceiptsAsync();
        Task<FinishedGoodsReceiptSummaryDto> GetReceiptSummaryAsync();
        Task<FinishedGoodsReceiptActionResultDto> CreateFinishedGoodsReceiptAsync(FinishedGoodsReceiptCreateDto receipt);
        Task<ProductionPlanForReceiptDto?> GetProductionPlanForReceiptAsync(int planId);
        Task<IEnumerable<dynamic>> GetActiveProductionPlansAsync();
        Task<IEnumerable<dynamic>> GetActiveStaffAsync();
    }
}

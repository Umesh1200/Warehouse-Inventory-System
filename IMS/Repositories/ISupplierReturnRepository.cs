using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public interface ISupplierReturnRepository
    {
        Task<IEnumerable<SupplierReturnDto>> GetAllReturnsAsync();
        Task<SupplierReturnDto?> GetReturnByIdAsync(int returnId);
        Task<IEnumerable<SupplierReturnItemDto>> GetReturnItemsAsync(int returnId);
        Task<int> CreateReturnAsync(SupplierReturnCreateDto returnDto, int createdBy);
        Task<bool> ConfirmReturnAsync(int returnId, int performedBy);
        Task<bool> UpdateCreditStatusAsync(int returnId, string creditStatus);
        Task<bool> CancelReturnAsync(int returnId);
        Task<SupplierReturnSummaryDto> GetReturnsSummaryAsync();
        Task<IEnumerable<dynamic>> GetQuarantineStockAsync();
        Task<bool> SupplierAuthorizeReturnAsync(int returnId, string rmaNumber, string carrier, string trackingNumber, string? creditNoteNo = null, decimal? creditAmount = null, string? replacementSKU = null, decimal? replacementQty = null, string? replacementTracking = null);
        Task<bool> RejectSupplierReturnAsync(int returnId, string rejectionReason);
    }
}

using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Services
{
    public interface ISupplierReturnService
    {
        Task<IEnumerable<SupplierReturnDto>> GetAllReturnsAsync();
        Task<SupplierReturnDto?> GetReturnByIdAsync(int returnId);
        Task<IEnumerable<SupplierReturnItemDto>> GetReturnItemsAsync(int returnId);
        Task<SupplierReturnActionResultDto> CreateReturnAsync(SupplierReturnCreateDto returnDto, int createdBy);
        Task<SupplierReturnActionResultDto> ConfirmReturnAsync(int returnId, int performedBy);
        Task<SupplierReturnActionResultDto> UpdateCreditStatusAsync(int returnId, string creditStatus);
        Task<SupplierReturnActionResultDto> CancelReturnAsync(int returnId);
        Task<SupplierReturnSummaryDto> GetReturnsSummaryAsync();
        Task<IEnumerable<dynamic>> GetQuarantineStockAsync();
        Task<SupplierReturnActionResultDto> SupplierAuthorizeReturnAsync(int returnId, string rmaNumber, string carrier, string trackingNumber, string? creditNoteNo = null, decimal? creditAmount = null, string? replacementSKU = null, decimal? replacementQty = null, string? replacementTracking = null);
        Task<SupplierReturnActionResultDto> RejectSupplierReturnAsync(int returnId, string rejectionReason);
    }

    public class SupplierReturnActionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? NewID { get; set; }
    }
}

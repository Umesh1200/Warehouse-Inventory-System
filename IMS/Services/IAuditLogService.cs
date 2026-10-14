using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Services
{
    public interface IAuditLogService
    {
        Task<IReadOnlyList<AuditLogDto>> GetStockMovementAuditAsync(AuditLogFilterDto filter);
        Task<AuditLogSummaryDto> GetAuditSummaryAsync(AuditLogFilterDto filter);
        Task<IReadOnlyList<DropdownOptionDto>> GetMovementTypesAsync();
        Task<IReadOnlyList<DropdownOptionDto>> GetProductsAsync();
        Task<IReadOnlyList<DropdownOptionDto>> GetUsersAsync();
    }
}

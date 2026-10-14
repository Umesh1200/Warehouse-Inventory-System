using IMS.Models.DTOs;
using IMS.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IAuditLogRepository _auditLogRepository;

        public AuditLogService(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository;
        }

        public async Task<IReadOnlyList<AuditLogDto>> GetStockMovementAuditAsync(AuditLogFilterDto filter)
        {
            return await _auditLogRepository.GetStockMovementAuditAsync(filter);
        }

        public async Task<AuditLogSummaryDto> GetAuditSummaryAsync(AuditLogFilterDto filter)
        {
            return await _auditLogRepository.GetAuditSummaryAsync(filter);
        }

        public async Task<IReadOnlyList<DropdownOptionDto>> GetMovementTypesAsync()
        {
            return await _auditLogRepository.GetMovementTypesAsync();
        }

        public async Task<IReadOnlyList<DropdownOptionDto>> GetProductsAsync()
        {
            return await _auditLogRepository.GetProductsAsync();
        }

        public async Task<IReadOnlyList<DropdownOptionDto>> GetUsersAsync()
        {
            return await _auditLogRepository.GetUsersAsync();
        }
    }
}

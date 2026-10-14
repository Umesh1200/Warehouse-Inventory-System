using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Services
{
    public interface ISupplierService
    {
        Task<IEnumerable<SupplierDto>> GetSuppliersAsync();
        Task<SupplierSummaryDto> GetSupplierSummaryAsync();
        Task<SupplierActionResultDto> CreateSupplierAsync(SupplierCreateDto supplier);
        Task<SupplierActionResultDto> UpdateSupplierAsync(SupplierUpdateDto supplier);
        Task<SupplierActionResultDto> DeactivateSupplierAsync(int supplierId);
    }
}

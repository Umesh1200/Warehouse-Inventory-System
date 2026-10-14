using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public interface ISupplierRepository
    {
        Task<IEnumerable<SupplierDto>> GetAllSuppliersAsync();
        Task<int> CreateSupplierAsync(SupplierCreateDto supplier);
        Task<SupplierDto?> GetSupplierByIdAsync(int supplierId);
        Task<bool> UpdateSupplierAsync(SupplierUpdateDto supplier);
        Task<bool> DeactivateSupplierAsync(int supplierId);
        Task<bool> IsSupplierCodeExistsAsync(string supplierCode);
        Task<bool> IsSupplierCodeExistsForOtherSupplierAsync(string supplierCode, int supplierId);
    }
}

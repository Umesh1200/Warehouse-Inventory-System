using IMS.Models.DTOs;
using IMS.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly ISupplierRepository _supplierRepository;
        private readonly IUserContext _userContext;

        public SupplierService(ISupplierRepository supplierRepository, IUserContext userContext)
        {
            _supplierRepository = supplierRepository;
            _userContext = userContext;
        }

        public async Task<IEnumerable<SupplierDto>> GetSuppliersAsync()
        {
            var suppliers = await _supplierRepository.GetAllSuppliersAsync();
            if (_userContext.SupplierID.HasValue)
            {
                suppliers = suppliers.Where(s => s.SupplierID == _userContext.SupplierID.Value);
            }
            return suppliers;
        }


        public async Task<SupplierSummaryDto> GetSupplierSummaryAsync()
        {
            var suppliers = await _supplierRepository.GetAllSuppliersAsync();
            var suppliersList = suppliers.ToList();

            return new SupplierSummaryDto
            {
                TotalSuppliers = suppliersList.Count,
                ActiveSuppliers = suppliersList.Count(s => s.Status == "Active"),
                PendingReview = suppliersList.Count(s => s.Status == "Pending Review"),
                AverageLeadTime = suppliersList.Any() ? (int)suppliersList.Average(s => s.LeadTimeDays) : 0
            };
        }

        public async Task<SupplierActionResultDto> CreateSupplierAsync(SupplierCreateDto supplier)
        {
            if (string.IsNullOrWhiteSpace(supplier.SupplierCode)) return new SupplierActionResultDto { Success = false, Message = "Supplier Code is required." };
            if (string.IsNullOrWhiteSpace(supplier.SupplierName)) return new SupplierActionResultDto { Success = false, Message = "Supplier Name is required." };
            if (supplier.LeadTimeDays < 0) return new SupplierActionResultDto { Success = false, Message = "Lead Time Days cannot be negative." };
            
            if (!string.IsNullOrWhiteSpace(supplier.Email) && !supplier.Email.Contains("@")) 
                return new SupplierActionResultDto { Success = false, Message = "Invalid email format." };

            if (await _supplierRepository.IsSupplierCodeExistsAsync(supplier.SupplierCode))
            {
                return new SupplierActionResultDto { Success = false, Message = $"Supplier Code '{supplier.SupplierCode}' already exists." };
            }

            try
            {
                int newId = await _supplierRepository.CreateSupplierAsync(supplier);
                return new SupplierActionResultDto { Success = true, Message = "Supplier created successfully.", NewID = newId };
            }
            catch (Exception ex)
            {
                return new SupplierActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<SupplierActionResultDto> UpdateSupplierAsync(SupplierUpdateDto supplier)
        {
            if (supplier.SupplierID <= 0) return new SupplierActionResultDto { Success = false, Message = "Invalid Supplier ID." };
            if (string.IsNullOrWhiteSpace(supplier.SupplierCode)) return new SupplierActionResultDto { Success = false, Message = "Supplier Code is required." };
            if (string.IsNullOrWhiteSpace(supplier.SupplierName)) return new SupplierActionResultDto { Success = false, Message = "Supplier Name is required." };
            if (supplier.LeadTimeDays < 0) return new SupplierActionResultDto { Success = false, Message = "Lead Time Days cannot be negative." };

            if (!string.IsNullOrWhiteSpace(supplier.Email) && !supplier.Email.Contains("@")) 
                return new SupplierActionResultDto { Success = false, Message = "Invalid email format." };

            if (await _supplierRepository.IsSupplierCodeExistsForOtherSupplierAsync(supplier.SupplierCode, supplier.SupplierID))
            {
                return new SupplierActionResultDto { Success = false, Message = $"Supplier Code '{supplier.SupplierCode}' already exists." };
            }

            try
            {
                bool success = await _supplierRepository.UpdateSupplierAsync(supplier);
                if (success)
                    return new SupplierActionResultDto { Success = true, Message = "Supplier updated successfully." };
                else
                    return new SupplierActionResultDto { Success = false, Message = "Supplier not found or no changes made." };
            }
            catch (Exception ex)
            {
                return new SupplierActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<SupplierActionResultDto> DeactivateSupplierAsync(int supplierId)
        {
            if (supplierId <= 0) return new SupplierActionResultDto { Success = false, Message = "Invalid Supplier ID." };

            try
            {
                bool success = await _supplierRepository.DeactivateSupplierAsync(supplierId);
                if (success)
                    return new SupplierActionResultDto { Success = true, Message = "Supplier deactivated successfully." };
                else
                    return new SupplierActionResultDto { Success = false, Message = "Supplier not found or already inactive." };
            }
            catch (Exception ex)
            {
                return new SupplierActionResultDto { Success = false, Message = ex.Message };
            }
        }
    }
}

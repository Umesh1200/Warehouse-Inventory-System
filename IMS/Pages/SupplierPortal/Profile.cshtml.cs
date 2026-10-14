using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using IMS.Services;
using IMS.Models.DTOs;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace IMS.Pages.SupplierPortal
{
    public class ProfileModel : PageModel
    {
        private readonly ISupplierService _supplierService;
        private readonly IUserContext _userContext;

        public string SuccessMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public SupplierDto SupplierDetails { get; set; } = new();

        [BindProperty]
        public SupplierUpdateDto EditProfile { get; set; } = new();

        public ProfileModel(ISupplierService supplierService, IUserContext userContext)
        {
            _supplierService = supplierService;
            _userContext = userContext;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!_userContext.SupplierID.HasValue)
            {
                return RedirectToPage("/Account/AccessDenied");
            }

            await LoadProfileAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!_userContext.SupplierID.HasValue)
            {
                return RedirectToPage("/Account/AccessDenied");
            }

            // Load current details from database first to make sure we don't modify locked fields
            var suppliers = await _supplierService.GetSuppliersAsync();
            var current = suppliers.FirstOrDefault(s => s.SupplierID == _userContext.SupplierID.Value);
            if (current == null)
            {
                ErrorMessage = "Supplier details not found.";
                return Page();
            }

            // Populate the update DTO with read-only fields from the database
            EditProfile.SupplierID = current.SupplierID;
            EditProfile.SupplierCode = current.SupplierCode;
            EditProfile.SupplierName = current.SupplierName;
            EditProfile.SupplierType = current.SupplierType;
            EditProfile.LeadTimeDays = current.LeadTimeDays;
            EditProfile.PaymentTerms = current.PaymentTerms;
            EditProfile.Status = current.Status;

            // Submit update via service
            var result = await _supplierService.UpdateSupplierAsync(EditProfile);
            if (result.Success)
            {
                TempData["SuccessMessage"] = "Profile details updated successfully.";
                return RedirectToPage();
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadProfileAsync();
                return Page();
            }
        }

        private async Task LoadProfileAsync()
        {
            if (TempData["SuccessMessage"] != null)
            {
                SuccessMessage = TempData["SuccessMessage"]?.ToString() ?? "";
            }

            if (!_userContext.SupplierID.HasValue) return;

            var suppliers = await _supplierService.GetSuppliersAsync();
            var supplier = suppliers.FirstOrDefault(s => s.SupplierID == _userContext.SupplierID.Value);
            if (supplier != null)
            {
                SupplierDetails = supplier;

                // Prepopulate form fields
                EditProfile = new SupplierUpdateDto
                {
                    SupplierID = supplier.SupplierID,
                    ContactPerson = supplier.ContactPerson,
                    Phone = supplier.Phone,
                    Email = supplier.Email,
                    Address = supplier.Address,
                    ReturnPolicy = supplier.ReturnPolicy,
                    ReturnEmail = supplier.ReturnEmail,
                    PreferredCarrier = supplier.PreferredCarrier
                };
            }
            else
            {
                ErrorMessage = "Unable to retrieve supplier profile details.";
            }
        }
    }
}

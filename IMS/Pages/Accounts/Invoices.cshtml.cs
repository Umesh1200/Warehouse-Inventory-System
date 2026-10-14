using IMS.Models.DTOs;
using IMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.Pages.Accounts
{
    public class InvoicesModel : PageModel
    {
        private readonly IProcurementWorkflowService _service;
        private readonly IUserContext _userContext;

        public InvoicesModel(IProcurementWorkflowService service, IUserContext userContext)
        {
            _service = service;
            _userContext = userContext;
        }

        public IReadOnlyList<SupplierInvoiceDto> Invoices { get; set; } = [];
        public IReadOnlyList<ProcurementOrderWorkflowDto> PurchaseOrders { get; set; } = [];
        public List<string> Statuses { get; set; } = [];
        public List<string> MatchStatuses { get; set; } = [];
        public List<string> Suppliers { get; set; } = [];

        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        [BindProperty] public SupplierInvoiceCreateDto NewInvoice { get; set; } = new();
        [BindProperty] public SupplierPaymentCreateDto NewPayment { get; set; } = new();
        [BindProperty] public int ActionInvoiceID { get; set; }
        [BindProperty] public string ActionStatus { get; set; } = string.Empty;

        public async Task OnGetAsync() => await LoadAsync();

        public async Task<IActionResult> OnPostCreateAsync()
        {
            NewInvoice.RecordedBy = _userContext.CurrentUserID;
            var result = await _service.CreateInvoiceAsync(NewInvoice);
            return await CompleteAsync(result);
        }

        public async Task<IActionResult> OnPostStatusAsync()
        {
            var result = await _service.SetInvoiceStatusAsync(ActionInvoiceID, ActionStatus, _userContext.CurrentUserID);
            return await CompleteAsync(result);
        }

        public async Task<IActionResult> OnPostPaymentAsync()
        {
            NewPayment.RecordedBy = _userContext.CurrentUserID;
            var result = await _service.RecordPaymentAsync(NewPayment);
            return await CompleteAsync(result);
        }

        private async Task<IActionResult> CompleteAsync(ProcurementActionResultDto result)
        {
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage();
            }
            ErrorMessage = result.Message;
            await LoadAsync();
            return Page();
        }

        private async Task LoadAsync()
        {
            SuccessMessage = TempData["SuccessMessage"]?.ToString() ?? string.Empty;
            try
            {
                Invoices = await _service.GetInvoicesAsync();
                PurchaseOrders = await _service.GetOrdersAsync();

                Statuses = ["All Statuses", "Pending Review", "Approved", "Partially Paid", "Paid", "On Hold", "Cancelled"];
                MatchStatuses = ["All Match Statuses", "Matched", "Variance", "Awaiting QC", "Awaiting Receipt"];

                Suppliers = ["All Suppliers"];
                Suppliers.AddRange(Invoices.Select(i => i.SupplierName).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Accounts workflow is unavailable. {ex.Message}";
            }
        }
    }
}

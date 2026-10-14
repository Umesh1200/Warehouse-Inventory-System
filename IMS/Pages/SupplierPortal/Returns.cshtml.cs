using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using IMS.Services;
using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace IMS.Pages.SupplierPortal
{
    public class ReturnsModel : PageModel
    {
        private readonly ISupplierReturnService _returnService;
        private readonly IUserContext _userContext;

        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public List<ReturnSummaryCard> SummaryCards { get; set; } = new();
        public List<SupplierReturnRow> ReturnsList { get; set; } = new();
        public SupplierReturnRow? SelectedReturn { get; set; }
        public List<SupplierReturnItemRow> SelectedItems { get; set; } = new();

        [BindProperty]
        public int AuthorizeReturnID { get; set; }

        [BindProperty]
        public string AuthorizeRMANumber { get; set; } = string.Empty;

        [BindProperty]
        public string AuthorizeCarrier { get; set; } = string.Empty;

        [BindProperty]
        public string AuthorizeTracking { get; set; } = string.Empty;

        [BindProperty]
        public string AuthorizeCreditNoteNo { get; set; } = string.Empty;

        [BindProperty]
        public decimal? AuthorizeCreditAmount { get; set; }

        [BindProperty]
        public int RejectReturnID { get; set; }

        [BindProperty]
        public string RejectReturnReason { get; set; } = string.Empty;

        [BindProperty]
        public string AuthorizeReplacementSKU { get; set; } = string.Empty;

        [BindProperty]
        public decimal? AuthorizeReplacementQty { get; set; }

        [BindProperty]
        public string AuthorizeReplacementTracking { get; set; } = string.Empty;

        public ReturnsModel(ISupplierReturnService returnService, IUserContext userContext)
        {
            _returnService = returnService;
            _userContext = userContext;
        }

        public async Task OnGetAsync(int? returnId)
        {
            await LoadDataAsync(returnId);
        }

        public async Task<IActionResult> OnPostSupplierAuthorizeAsync()
        {
            if (!_userContext.SupplierID.HasValue)
            {
                return RedirectToPage("/Account/AccessDenied");
            }

            if (AuthorizeReturnID <= 0 || string.IsNullOrWhiteSpace(AuthorizeRMANumber))
            {
                ErrorMessage = "Return ID and RMA Number are required.";
                await LoadDataAsync(null);
                return Page();
            }

            var result = await _returnService.SupplierAuthorizeReturnAsync(
                AuthorizeReturnID, 
                AuthorizeRMANumber, 
                AuthorizeCarrier, 
                AuthorizeTracking, 
                AuthorizeCreditNoteNo, 
                AuthorizeCreditAmount,
                AuthorizeReplacementSKU,
                AuthorizeReplacementQty,
                AuthorizeReplacementTracking);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage(new { returnId = AuthorizeReturnID });
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadDataAsync(AuthorizeReturnID);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostSupplierRejectAsync()
        {
            if (!_userContext.SupplierID.HasValue)
            {
                return RedirectToPage("/Account/AccessDenied");
            }

            if (RejectReturnID <= 0 || string.IsNullOrWhiteSpace(RejectReturnReason))
            {
                ErrorMessage = "Return ID and rejection reason are required.";
                await LoadDataAsync(null);
                return Page();
            }

            var result = await _returnService.RejectSupplierReturnAsync(RejectReturnID, RejectReturnReason);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage(new { returnId = RejectReturnID });
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadDataAsync(RejectReturnID);
                return Page();
            }
        }

        private async Task LoadDataAsync(int? selectedReturnId)
        {
            try
            {
                if (TempData["SuccessMessage"] != null)
                {
                    SuccessMessage = TempData["SuccessMessage"]?.ToString() ?? "";
                }

                if (!_userContext.SupplierID.HasValue) return;

                // Fetch Returns list (automatically filtered by SupplierID in Return Service)
                var returnsData = (await _returnService.GetAllReturnsAsync()).ToList();
                var summary = await _returnService.GetReturnsSummaryAsync();

                // Build summary cards
                SummaryCards = new List<ReturnSummaryCard>
                {
                    new ReturnSummaryCard { Title = "Total Returns", Value = summary.TotalReturns.ToString(), Icon = "keyboard_return", ColorClass = "primary" },
                    new ReturnSummaryCard { Title = "Shipped Returns", Value = summary.ShippedReturns.ToString(), Icon = "local_shipping", ColorClass = "info" },
                    new ReturnSummaryCard { Title = "Pending Credits", Value = summary.PendingCredit.ToString(), Icon = "pending_actions", ColorClass = "warning" },
                    new ReturnSummaryCard { Title = "Total Value Returned", Value = $"Rs. {summary.TotalValue:N2}", Icon = "payments", ColorClass = "success" }
                };

                ReturnsList = returnsData.Select(r => new SupplierReturnRow
                {
                    ReturnID = r.ReturnID,
                    ReturnNo = r.ReturnNo,
                    SupplierID = r.SupplierID,
                    SupplierName = r.SupplierName,
                    RMANumber = r.RMANumber,
                    ReturnDate = r.ReturnDate.ToString("MMM dd, yyyy"),
                    Carrier = r.Carrier,
                    TrackingNumber = r.TrackingNumber,
                    CreditStatus = r.CreditStatus,
                    Status = r.Status,
                    Remarks = r.Remarks,
                    CreditNoteNo = r.CreditNoteNo,
                    CreditAmount = r.CreditAmount,
                    CreatorName = r.CreatorName,
                    CreatedAt = r.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    RejectionReason = r.RejectionReason,
                    ReplacementSKU = r.ReplacementSKU,
                    ReplacementQty = r.ReplacementQty,
                    ReplacementTracking = r.ReplacementTracking,
                    IsSelected = selectedReturnId.HasValue && r.ReturnID == selectedReturnId.Value
                }).ToList();

                // Handle selected return details
                SelectedReturn = ReturnsList.FirstOrDefault(r => r.IsSelected) ?? ReturnsList.FirstOrDefault();
                if (SelectedReturn != null)
                {
                    SelectedReturn.IsSelected = true;
                    var items = await _returnService.GetReturnItemsAsync(SelectedReturn.ReturnID);
                    SelectedItems = items.Select(i => new SupplierReturnItemRow
                    {
                        ReturnItemID = i.ReturnItemID,
                        ProductID = i.ProductID,
                        SKU = i.SKU,
                        ProductName = i.ProductName,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        TotalPrice = i.Quantity * i.UnitPrice,
                        Reason = i.Reason,
                        SourceShelfID = i.SourceShelfID,
                        SourceShelfCode = i.SourceShelfCode
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load data: {ex.Message}";
            }
        }
    }

    public class ReturnSummaryCard
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
    }

    public class SupplierReturnRow
    {
        public int ReturnID { get; set; }
        public string ReturnNo { get; set; } = string.Empty;
        public int SupplierID { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string RMANumber { get; set; } = string.Empty;
        public string ReturnDate { get; set; } = string.Empty;
        public string Carrier { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
        public string CreditStatus { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string CreditNoteNo { get; set; } = string.Empty;
        public decimal? CreditAmount { get; set; }
        public string CreatorName { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string RejectionReason { get; set; } = string.Empty;
        public string ReplacementSKU { get; set; } = string.Empty;
        public decimal? ReplacementQty { get; set; }
        public string ReplacementTracking { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    public class SupplierReturnItemRow
    {
        public int ReturnItemID { get; set; }
        public int ProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int? SourceShelfID { get; set; }
        public string SourceShelfCode { get; set; } = string.Empty;
    }
}

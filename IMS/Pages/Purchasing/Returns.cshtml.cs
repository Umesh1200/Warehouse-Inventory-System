using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using IMS.Services;
using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System;

namespace IMS.Pages.Purchasing
{
    public class ReturnsModel : PageModel
    {
        private readonly ISupplierReturnService _returnService;
        private readonly ISupplierService _supplierService;
        private readonly IUserContext _userContext;
        public IUserContext UserContext { get; }


        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public ReturnsModel(
            ISupplierReturnService returnService,
            ISupplierService supplierService,
            IUserContext userContext)
        {
            _returnService = returnService;
            _supplierService = supplierService;
            _userContext = userContext;
            UserContext = userContext;
        }


        public List<ReturnSummaryCard> SummaryCards { get; set; } = new();
        public List<SupplierReturnRow> ReturnsList { get; set; } = new();
        public SupplierReturnRow? SelectedReturn { get; set; }
        public List<SupplierReturnItemRow> SelectedItems { get; set; } = new();
        public List<SupplierDto> ActiveSuppliersList { get; set; } = new();
        public List<dynamic> QuarantineStockList { get; set; } = new();
        public string ActiveSuppliersJson { get; set; } = "[]";
        public string QuarantineStockJson { get; set; } = "[]";

        [BindProperty]
        public SupplierReturnCreateDto NewReturn { get; set; } = new();

        [BindProperty]
        public string NewReturnItemsJson { get; set; } = "[]";

        [BindProperty]
        public int ConfirmReturnID { get; set; }

        [BindProperty]
        public int CancelReturnID { get; set; }

        [BindProperty]
        public int CreditReturnID { get; set; }

        [BindProperty]
        public string CreditStatus { get; set; } = "Pending";

        [BindProperty]
        public int AuthorizeReturnID { get; set; }

        [BindProperty]
        public string AuthorizeRMANumber { get; set; } = string.Empty;

        [BindProperty]
        public string AuthorizeCarrier { get; set; } = string.Empty;

        [BindProperty]
        public string AuthorizeTracking { get; set; } = string.Empty;

        public async Task OnGetAsync(int? returnId)
        {
            await LoadDataAsync(returnId);
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!string.IsNullOrWhiteSpace(NewReturnItemsJson))
            {
                try
                {
                    NewReturn.Items = JsonSerializer.Deserialize<List<SupplierReturnItemCreateDto>>(
                        NewReturnItemsJson, 
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) ?? new();
                }
                catch
                {
                    ErrorMessage = "Invalid return items format.";
                    await LoadDataAsync(null);
                    return Page();
                }
            }

            var result = await _returnService.CreateReturnAsync(NewReturn, _userContext.CurrentUserID);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage(new { returnId = result.NewID });
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadDataAsync(null);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostConfirmAsync()
        {
            if (ConfirmReturnID <= 0)
            {
                ErrorMessage = "Invalid Return ID.";
                await LoadDataAsync(null);
                return Page();
            }

            var result = await _returnService.ConfirmReturnAsync(ConfirmReturnID, _userContext.CurrentUserID);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage(new { returnId = ConfirmReturnID });
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadDataAsync(ConfirmReturnID);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostCancelAsync()
        {
            if (CancelReturnID <= 0)
            {
                ErrorMessage = "Invalid Return ID.";
                await LoadDataAsync(null);
                return Page();
            }

            var result = await _returnService.CancelReturnAsync(CancelReturnID);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage(new { returnId = CancelReturnID });
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadDataAsync(CancelReturnID);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostUpdateCreditAsync()
        {
            if (CreditReturnID <= 0)
            {
                ErrorMessage = "Invalid Return ID.";
                await LoadDataAsync(null);
                return Page();
            }

            var result = await _returnService.UpdateCreditStatusAsync(CreditReturnID, CreditStatus);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage(new { returnId = CreditReturnID });
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadDataAsync(CreditReturnID);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostSupplierAuthorizeAsync()
        {
            if (AuthorizeReturnID <= 0 || string.IsNullOrWhiteSpace(AuthorizeRMANumber))
            {
                ErrorMessage = "Return ID and RMA Number are required.";
                await LoadDataAsync(null);
                return Page();
            }

            var result = await _returnService.SupplierAuthorizeReturnAsync(AuthorizeReturnID, AuthorizeRMANumber, AuthorizeCarrier, AuthorizeTracking);

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

        private async Task LoadDataAsync(int? selectedReturnId)
        {
            try
            {
                if (TempData["SuccessMessage"] != null)
                {
                    SuccessMessage = TempData["SuccessMessage"]?.ToString() ?? "";
                }

                // Fetch Suppliers
                var suppliers = await _supplierService.GetSuppliersAsync();
                ActiveSuppliersList = suppliers.Where(s => s.Status == "Active").ToList();
                ActiveSuppliersJson = JsonSerializer.Serialize(ActiveSuppliersList, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                // Fetch Quarantine stock
                var qStock = await _returnService.GetQuarantineStockAsync();
                QuarantineStockList = qStock.ToList();
                QuarantineStockJson = JsonSerializer.Serialize(QuarantineStockList, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                // Fetch Returns logs
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
                    CreatorName = r.CreatorName,
                    CreatedAt = r.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
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
                ReturnsList = new List<SupplierReturnRow>();
                SelectedItems = new List<SupplierReturnItemRow>();
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
        public string CreatorName { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
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

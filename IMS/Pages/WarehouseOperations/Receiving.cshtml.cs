using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using IMS.Services;
using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace IMS.Pages.WarehouseOperations
{
    public class ReceivingModel : PageModel
    {
        private readonly IReceivingService _receivingService;
        private readonly IUserContext _userContext;
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public ReceivingModel(IReceivingService receivingService, IUserContext userContext)
        {
            _receivingService = receivingService;
            _userContext = userContext;
        }

        public List<ReceivingSummaryCard> SummaryCards { get; set; } = new();
        public List<ReceivingRow> ReceivingTasks { get; set; } = new();
        public List<string> Suppliers { get; set; } = new();

        [BindProperty]
        public ReceivingCreateDto NewReceipt { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        public async Task<IActionResult> OnPostReceiveAsync()
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                ErrorMessage = "Validation failed: " + string.Join(", ", errors);
                await LoadDataAsync();
                return Page();
            }

            NewReceipt.ReceivedBy = _userContext.CurrentUserID;

            var result = await _receivingService.ReceiveItemAsync(NewReceipt);
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage();
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadDataAsync();
                return Page();
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                if (TempData["SuccessMessage"] != null)
                {
                    SuccessMessage = TempData["SuccessMessage"]?.ToString() ?? "";
                }

                // Fetch live data from SQL
                var receivingData = (await _receivingService.GetReceivingTasksAsync()).ToList();
                var summaryData = await _receivingService.GetReceivingSummaryAsync();

                Suppliers = new List<string> { "All Suppliers" };
                Suppliers.AddRange(receivingData.Select(t => t.SupplierName).Distinct());

                // Map Summary Cards
                SummaryCards = new List<ReceivingSummaryCard>
                {
                    new ReceivingSummaryCard { Title = "Expected Today", Value = $"{summaryData.ExpectedTodayCount} Items", Icon = "calendar_today", ColorClass = "primary" },
                    new ReceivingSummaryCard { Title = "Received Items", Value = summaryData.TotalReceipts.ToString(), Icon = "check_circle", ColorClass = "success" },
                    new ReceivingSummaryCard { Title = "Pending QC", Value = summaryData.QCRequiredCount.ToString(), Icon = "fact_check", ColorClass = "warning" },
                    new ReceivingSummaryCard { Title = "Damaged / Rejected", Value = summaryData.TotalDamagedRejected.ToString("N0"), Icon = "report_problem", ColorClass = "danger" }
                };

                // Map Receiving Rows
                ReceivingTasks = receivingData.Select(t => new ReceivingRow
                {
                    ReceiptID = t.ReceiptID,
                    POItemID = t.POItemID,
                    PONumber = t.PONumber,
                    Supplier = t.SupplierName,
                    Product = t.ProductName,
                    SKU = t.SKU,
                    OrderedQty = (int)t.OrderedQty,
                    ReceivedQty = (int)t.ReceivedQty,
                    RemainingQty = (int)(t.OrderedQty - t.ReceivedQty),
                    DamagedQty = (int)t.DamagedQty,
                    RejectedQty = (int)t.RejectedQty,
                    BatchNumber = t.BatchNumber,
                    ReceivedBy = string.IsNullOrWhiteSpace(t.ReceivedByName) ? "-" : t.ReceivedByName,
                    ExpectedDate = t.ExpectedDate?.ToString("yyyy-MM-dd") ?? "-",
                    ReceiptDate = t.ReceiptDate?.ToString("yyyy-MM-dd HH:mm") ?? "-",
                    ProgressPercentage = t.OrderedQty > 0 ? (int)Math.Min(100, (t.ReceivedQty / t.OrderedQty) * 100) : 0,
                    Status = t.Status,
                    StatusColor = GetStatusColor(t.Status),
                    ReceiptDateTime = t.ReceiptDate
                }).ToList();
            }
            catch (Exception)
            {
                ErrorMessage = "Database connection failed. Showing offline mode.";
                ReceivingTasks = new List<ReceivingRow>();
            }
        }

        private string GetStatusColor(string status)
        {
            return status switch
            {
                "Received" => "success",
                "Accepted" => "success",
                "QC Required" => "warning",
                "Partial" => "warning",
                "Expected" => "info",
                "Rejected" => "danger",
                "Cancelled" => "danger",
                _ => "muted"
            };
        }
    }

    public class ReceivingSummaryCard
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
    }

    public class ReceivingRow
    {
        public int? ReceiptID { get; set; }
        public int POItemID { get; set; }
        public string PONumber { get; set; } = string.Empty;
        public string Supplier { get; set; } = string.Empty;
        public string Product { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int OrderedQty { get; set; }
        public int ReceivedQty { get; set; }
        public int RemainingQty { get; set; }
        public int DamagedQty { get; set; }
        public int RejectedQty { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public string ReceivedBy { get; set; } = string.Empty;
        public string ExpectedDate { get; set; } = string.Empty;
        public string ReceiptDate { get; set; } = string.Empty;
        public int ProgressPercentage { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public DateTime? ReceiptDateTime { get; set; }
    }
}

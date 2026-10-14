using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using IMS.Services;
using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace IMS.Pages.Production
{
    public class FinishedGoodsReceiptModel : PageModel
    {
        private readonly IFinishedGoodsReceiptService _fgService;
        private readonly IUserContext _userContext;
        private readonly ISystemSettingService _settingService;
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public FinishedGoodsReceiptModel(
            IFinishedGoodsReceiptService fgService, 
            IUserContext userContext,
            ISystemSettingService settingService)
        {
            _fgService = fgService;
            _userContext = userContext;
            _settingService = settingService;
        }


        public ReceiptSummary Summary { get; set; } = new();
        public List<ReceiptItemRow> Items { get; set; } = new();
        public List<ReceiptHistoryRow> RecentReceipts { get; set; } = new();
        public List<string> QCInspectors { get; set; } = new();

        public List<dynamic> ActivePlansList { get; set; } = new();
        public List<dynamic> StaffList { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public int? PlanID { get; set; }

        [BindProperty]
        public FinishedGoodsReceiptCreateDto NewReceipt { get; set; } = new();

        private async Task<bool> IsProductionEnabledAsync()
        {
            var settings = await _settingService.GetSettingsAsync();
            var productionSetting = settings.FirstOrDefault(s => s.SettingKey == "Production.ProductionPagesEnabled");
            return productionSetting?.SettingValue?.ToLower() == "true";
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!await IsProductionEnabledAsync()) return Forbid();
            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!await IsProductionEnabledAsync()) return Forbid();
            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return Page();
            }

            NewReceipt.ReceivedBy = _userContext.CurrentUserID;

            var result = await _fgService.CreateFinishedGoodsReceiptAsync(NewReceipt);
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage(new { PlanID = NewReceipt.PlanID });
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

                QCInspectors = new List<string> { "Select QC Inspector", "Ramesh Adhikari (Senior QC)", "Sita Sharma (Associate QC)", "Binod Chaudhary (External Auditor)" };
                ActivePlansList = (await _fgService.GetActiveProductionPlansAsync()).ToList();
                StaffList = (await _fgService.GetActiveStaffAsync()).ToList();
                if (PlanID.HasValue)
                {
                    NewReceipt.PlanID = PlanID.Value;
                }

                // Fetch live data from SQL
                var receiptsData = await _fgService.GetReceiptsAsync();
                var summaryData = await _fgService.GetReceiptSummaryAsync();

                var filteredReceipts = receiptsData.AsQueryable();

                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    var term = SearchTerm.ToLower();
                    filteredReceipts = filteredReceipts.Where(r => 
                        (r.ReceiptNumber != null && r.ReceiptNumber.ToLower().Contains(term)) ||
                        (r.BatchNumber != null && r.BatchNumber.ToLower().Contains(term)) ||
                        (r.SKU != null && r.SKU.ToLower().Contains(term)) ||
                        (r.ProductName != null && r.ProductName.ToLower().Contains(term))
                    );
                }

                var listData = filteredReceipts.ToList();
                if (PlanID.HasValue)
                {
                    listData = listData.Where(r => r.PlanID == PlanID.Value).ToList();
                }

                if (listData.Any())
                {
                    // Map Meta Info from the most recent receipt for context
                    var first = listData.First();
                    Summary = new ReceiptSummary
                    {
                        Location = "Standard Receive Zone", // Aggregate or from first
                        TotalValue = $"NPR {summaryData.TotalAcceptedQty * 100:N0}", // Mock value
                        ProductionStatus = first.ProductionStatus,
                        ReceiptNumber = first.ReceiptNumber,
                        BatchNumber = first.BatchNumber,
                        ProductionLine = "Production Line 1"
                    };

                    // Map Items from current context
                    Items = listData.Where(r => r.BatchNumber == first.BatchNumber).Select(i => new ReceiptItemRow
                    {
                        ItemID = i.SKU,
                        ProductName = i.ProductName,
                        TargetQty = i.ProducedQty, // In receipt context, produced is target for accepted
                        ActualQty = i.AcceptedQty,
                        QCPriority = "MEDIUM",
                        PriorityColor = "info"
                    }).ToList();

                    // Map History
                    RecentReceipts = listData.Select(h => new ReceiptHistoryRow
                    {
                        Timestamp = h.ReceiptDate.ToString("MMM dd, HH:mm"),
                        BatchID = h.BatchNumber,
                        User = h.ReceivedByName,
                        Status = h.Status,
                        StatusColor = GetStatusColor(h.Status),
                        Value = $"NPR {h.AcceptedQty * 100:N0}"
                    }).Take(15).ToList();
                }
                else if (PlanID.HasValue)
                {
                    var selectedPlan = await _fgService.GetProductionPlanForReceiptAsync(PlanID.Value);
                    Summary = new ReceiptSummary
                    {
                        Location = "Finished goods receiving",
                        TotalValue = "NPR 0",
                        ProductionStatus = selectedPlan == null ? "Plan not found" : "Ready for receipt",
                        ReceiptNumber = selectedPlan?.PlanNumber ?? "",
                        BatchNumber = selectedPlan?.PlanNumber ?? "",
                        ProductionLine = selectedPlan == null ? "" : $"[{selectedPlan.SKU}] {selectedPlan.ProductName}"
                    };
                }
            }
            catch (Exception)
            {
                ErrorMessage = "Database connection failed. Showing offline mode.";
                RecentReceipts = new List<ReceiptHistoryRow>();
            }
        }

        private string GetStatusColor(string status)
        {
            return status switch
            {
                "Putaway Completed" => "success",
                "Completed" => "success",
                "Pending QC" => "warning",
                "QC Required" => "warning",
                "Rejected" => "danger",
                _ => "info"
            };
        }
    }

    public class ReceiptSummary
    {
        public string Location { get; set; } = string.Empty;
        public string TotalValue { get; set; } = string.Empty;
        public string ProductionStatus { get; set; } = string.Empty;
        public string ReceiptNumber { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public string ProductionLine { get; set; } = string.Empty;
    }

    public class ReceiptItemRow
    {
        public string ItemID { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal TargetQty { get; set; }
        public decimal ActualQty { get; set; }
        public string QCPriority { get; set; } = string.Empty;
        public string PriorityColor { get; set; } = string.Empty;
    }

    public class ReceiptHistoryRow
    {
        public string Timestamp { get; set; } = string.Empty;
        public string BatchID { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}

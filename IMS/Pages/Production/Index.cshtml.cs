using IMS.Models.DTOs;
using IMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.Pages.Production
{
    public class IndexModel : PageModel
    {
        private readonly IProductionPlanningService _planningService;
        private readonly IFinishedGoodsReceiptService _receiptService;
        private readonly IMaterialIssueReturnService _issueService;
        private readonly IBomService _bomService;
        private readonly ISystemSettingService _settingService;

        public IndexModel(
            IProductionPlanningService planningService,
            IFinishedGoodsReceiptService receiptService,
            IMaterialIssueReturnService issueService,
            IBomService bomService,
            ISystemSettingService settingService)
        {
            _planningService = planningService;
            _receiptService = receiptService;
            _issueService = issueService;
            _bomService = bomService;
            _settingService = settingService;
        }

        public string ErrorMessage { get; set; } = string.Empty;
        public List<ProductionSummaryCard> SummaryCards { get; set; } = new();
        public List<ProductionPlanRowDto> ActiveWorkOrders { get; set; } = new();
        public List<MaterialIssueReturnDto> RecentIssues { get; set; } = new();
        public List<FinishedGoodsReceiptDto> RecentReceipts { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var settings = await _settingService.GetSettingsAsync();
            var productionSetting = settings.FirstOrDefault(s => s.SettingKey == "Production.ProductionPagesEnabled");
            if (productionSetting?.SettingValue?.ToLower() != "true") return Forbid();

            try
            {
                var planSummary = await _planningService.GetProductionPlanningSummaryAsync();
                var receiptSummary = await _receiptService.GetReceiptSummaryAsync();
                var allPlans = (await _planningService.GetProductionPlansAsync()).ToList();
                var allBoms = (await _bomService.GetBomsAsync()).ToList();
                var allIssues = (await _issueService.GetIssueReturnTasksAsync()).ToList();
                var allReceipts = (await _receiptService.GetReceiptsAsync()).ToList();

                int activeBatches = allPlans.Count(p => p.Status == "In Production" || p.Status == "Approved");
                int activeBomsCount = allBoms.Count(b => b.Status == "Active");
                decimal totalFgProduced = allReceipts.Sum(r => r.AcceptedQty);
                int readyPlans = allPlans.Count(p => p.Status != "Draft" && p.PendingMaterialIssues == 0);
                int totalActiveOrDraft = allPlans.Count(p => p.Status == "Draft" || p.Status == "Approved" || p.Status == "In Production");
                int readinessPct = totalActiveOrDraft > 0 ? (int)Math.Round((double)readyPlans / totalActiveOrDraft * 100) : 100;

                SummaryCards = new List<ProductionSummaryCard>
                {
                    new() { Title = "Active Work Orders", Value = activeBatches.ToString("N0"), Subtitle = $"{planSummary.InProductionCount} in assembly", Icon = "precision_manufacturing", ColorClass = "primary" },
                    new() { Title = "Material Readiness", Value = $"{readinessPct}%", Subtitle = $"{readyPlans} batches staged", Icon = "fact_check", ColorClass = "info" },
                    new() { Title = "Accepted Output", Value = totalFgProduced.ToString("N0"), Subtitle = "Total units manufactured", Icon = "inventory_2", ColorClass = "success" },
                    new() { Title = "Active BOM Recipes", Value = activeBomsCount.ToString("N0"), Subtitle = $"{allBoms.Count} total revisions", Icon = "account_tree", ColorClass = "warning" }
                };

                ActiveWorkOrders = allPlans
                    .Where(p => p.Status == "In Production" || p.Status == "Approved" || p.Status == "Draft")
                    .Take(6)
                    .Select(p => new ProductionPlanRowDto
                    {
                        PlanID = p.PlanID,
                        PlanNumber = p.PlanNumber,
                        SKU = p.SKU,
                        ProductName = p.ProductName,
                        BOMVersion = p.BOMVersion,
                        PlannedQty = p.PlannedQty,
                        ProducedQty = p.ProducedQty,
                        TotalAcceptedFinishedQty = p.TotalAcceptedFinishedQty,
                        Priority = p.Priority,
                        Status = p.Status,
                        SupervisorName = p.SupervisorName,
                        DueDate = p.DueDate,
                        ProgressPercent = p.PlannedQty > 0 ? (int)Math.Min(100, Math.Round((p.ProducedQty > 0 ? p.ProducedQty : p.TotalAcceptedFinishedQty) / p.PlannedQty * 100)) : 0
                    }).ToList();

                RecentIssues = allIssues.Take(5).ToList();
                RecentReceipts = allReceipts.Take(5).ToList();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Database error loading production dashboard: {ex.Message}";
                SummaryCards = new List<ProductionSummaryCard>
                {
                    new() { Title = "Active Work Orders", Value = "0", Subtitle = "0 in assembly", Icon = "precision_manufacturing", ColorClass = "primary" },
                    new() { Title = "Material Readiness", Value = "0%", Subtitle = "0 batches staged", Icon = "fact_check", ColorClass = "info" },
                    new() { Title = "Accepted Output", Value = "0", Subtitle = "0 units manufactured", Icon = "inventory_2", ColorClass = "success" },
                    new() { Title = "Active BOM Recipes", Value = "0", Subtitle = "0 revisions", Icon = "account_tree", ColorClass = "warning" }
                };
            }

            return Page();
        }
    }

    public class ProductionSummaryCard
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
    }

    public class ProductionPlanRowDto
    {
        public int PlanID { get; set; }
        public string PlanNumber { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string BOMVersion { get; set; } = string.Empty;
        public decimal PlannedQty { get; set; }
        public decimal ProducedQty { get; set; }
        public decimal TotalAcceptedFinishedQty { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string SupervisorName { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public int ProgressPercent { get; set; }
    }
}


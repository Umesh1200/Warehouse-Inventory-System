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
    public class PlanningModel : PageModel
    {
        private readonly IProductionPlanningService _planningService;
        private readonly ISystemSettingService _settingService;
        private readonly IUserContext _userContext;
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public PlanningModel(
            IProductionPlanningService planningService, 
            ISystemSettingService settingService,
            IUserContext userContext)
        {
            _planningService = planningService;
            _settingService = settingService;
            _userContext = userContext;
        }

        public List<KPICard> SummaryCards { get; set; } = new();
        public List<ProductionPlanRow> ActivePlans { get; set; } = new();

        public List<dynamic> ActiveBomsList { get; set; } = new();
        public List<dynamic> SupervisorList { get; set; } = new();

        [BindProperty]
        public ProductionPlanCreateDto NewPlan { get; set; } = new();

        [BindProperty]
        public ProductionPlanUpdateDto EditPlan { get; set; } = new();

        [BindProperty]
        public int ApprovePlanID { get; set; }

        [BindProperty]
        public int StartPlanID { get; set; }

        [BindProperty]
        public int CancelPlanID { get; set; }

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

        public async Task<IActionResult> OnGetBomInspectionAsync(int bomId, decimal plannedQty)
        {
            var inspection = await _planningService.GetBomInspectionAsync(bomId, plannedQty);
            if (inspection == null) return NotFound();
            return new JsonResult(inspection);
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!await IsProductionEnabledAsync()) return Forbid();
            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return Page();
            }

            var result = await _planningService.CreateProductionPlanAsync(NewPlan);
            
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

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return Page();
            }

            var result = await _planningService.UpdateProductionPlanAsync(EditPlan);

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

        public async Task<IActionResult> OnPostApproveAsync()
        {
            if (ApprovePlanID <= 0)
            {
                ErrorMessage = "Invalid Plan ID.";
                await LoadDataAsync();
                return Page();
            }

            var result = await _planningService.ApproveProductionPlanAsync(ApprovePlanID);
            
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

        public async Task<IActionResult> OnPostStartAsync()
        {
            if (StartPlanID <= 0)
            {
                ErrorMessage = "Invalid Plan ID.";
                await LoadDataAsync();
                return Page();
            }

            var result = await _planningService.StartProductionPlanAsync(StartPlanID);
            
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

        public async Task<IActionResult> OnPostCancelAsync()
        {
            if (CancelPlanID <= 0)
            {
                ErrorMessage = "Invalid Plan ID.";
                await LoadDataAsync();
                return Page();
            }

            var result = await _planningService.CancelProductionPlanAsync(CancelPlanID);
            
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

                ActiveBomsList = (await _planningService.GetActiveBomsAsync()).ToList();
                SupervisorList = (await _planningService.GetActiveSupervisorsAsync()).ToList();

                // Pre-populate NewPlan with auto-generated defaults
                if (string.IsNullOrWhiteSpace(NewPlan.PlanNumber))
                {
                    NewPlan.PlanNumber = $"PLN-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";
                }
                if (NewPlan.AssignedSupervisor == 0 && _userContext.CurrentUserID > 0)
                {
                    NewPlan.AssignedSupervisor = _userContext.CurrentUserID;
                }
                if (NewPlan.StartDate == default)
                {
                    NewPlan.StartDate = DateTime.Today;
                }
                if (NewPlan.DueDate == default)
                {
                    NewPlan.DueDate = DateTime.Today.AddDays(7);
                }

                // Fetch live data from SQL
                var plansData = (await _planningService.GetProductionPlansAsync()).ToList();
                var summaryData = await _planningService.GetProductionPlanningSummaryAsync();

                // Map Summary Cards
                SummaryCards = new List<KPICard>
                {
                    new KPICard { Title = "Active Runs", Value = summaryData.InProductionCount.ToString(), Icon = "precision_manufacturing", Trend = "", TrendUp = true, ColorClass = "info" },
                    new KPICard { Title = "Scheduled Batches", Value = summaryData.TotalPlans.ToString(), Icon = "event_note", ColorClass = "warning" },
                    new KPICard { Title = "Pending Check", Value = summaryData.PendingMaterialCheckCount.ToString(), Icon = "fact_check", IsCritical = summaryData.PendingMaterialCheckCount > 5, ColorClass = "danger" }
                };

                // Map Active Run Ledger
                ActivePlans = plansData.Select(p => new ProductionPlanRow
                {
                    PlanID = p.PlanID,
                    BOMID = p.BOMID,
                    AssignedSupervisor = p.AssignedSupervisor ?? 0,
                    RawStartDate = p.StartDate?.ToString("yyyy-MM-dd") ?? "",
                    RawDueDate = p.DueDate?.ToString("yyyy-MM-dd") ?? "",
                    BatchID = p.PlanNumber,
                    ProductName = p.ProductName,
                    Category = p.BOMVersion,
                    Priority = p.Priority,
                    CompletionEst = p.DueDate?.ToString("MMM dd, yyyy") ?? "TBD",
                    QtyPlanned = p.PlannedQty,
                    QtyText = $"{p.PlannedQty:N0} units",
                    Status = p.Status,
                    StatusColor = GetStatusColor(p.Status),
                    Icon = "inventory_2"
                }).ToList();
            }
            catch (Exception)
            {
                ErrorMessage = "Database connection failed. Showing offline mode.";
                ActivePlans = new List<ProductionPlanRow>();
            }
        }

        private string GetStatusColor(string status)
        {
            return status switch
            {
                "In Production" => "success",
                "Completed" => "success",
                "Material Check" => "warning",
                "QC" => "info",
                "Approved" => "primary",
                "Delayed" => "danger",
                "Cancelled" => "danger",
                _ => "muted"
            };
        }
    }

    public class KPICard
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Trend { get; set; } = string.Empty;
        public bool TrendUp { get; set; }
        public bool IsCritical { get; set; }
        public string ColorClass { get; set; } = string.Empty;
    }

    public class ProductionPlanRow
    {
        public int PlanID { get; set; }
        public int BOMID { get; set; }
        public int AssignedSupervisor { get; set; }
        public string RawStartDate { get; set; } = string.Empty;
        public string RawDueDate { get; set; } = string.Empty;
        public string BatchID { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string CompletionEst { get; set; } = string.Empty;
        public decimal QtyPlanned { get; set; }
        public string QtyText { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }
}

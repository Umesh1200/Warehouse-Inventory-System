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
    public class MaterialIssueReturnModel : PageModel
    {
        private readonly IMaterialIssueReturnService _productionService;
        private readonly IUserContext _userContext;
        private readonly ISystemSettingService _settingService;
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public MaterialIssueReturnModel(IMaterialIssueReturnService productionService, IUserContext userContext, ISystemSettingService settingService)
        {
            _productionService = productionService;
            _userContext = userContext;
            _settingService = settingService;
        }


        public List<MIRKPICard> SummaryCards { get; set; } = new();
        public List<MaterialIssueRow> IssueItems { get; set; } = new();
        public List<IssuedMaterial> IssuedList { get; set; } = new();
        public ProductionPlanHeader CurrentPlan { get; set; } = new();

        public List<dynamic> ActivePlansList { get; set; } = new();
        public List<dynamic> RawMaterialsList { get; set; } = new();
        public List<dynamic> ActiveShelvesList { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? PlanID { get; set; }

        [BindProperty]
        public MaterialIssueCreateDto NewIssueRequest { get; set; } = new();

        [BindProperty]
        public MaterialIssueConfirmDto ConfirmIssue { get; set; } = new();

        [BindProperty]
        public MaterialReturnDto NewMaterialReturn { get; set; } = new();

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

        public async Task<IActionResult> OnPostCreateIssueAsync()
        {
            if (!await IsProductionEnabledAsync()) return Forbid();
            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return Page();
            }

            NewIssueRequest.IssuedBy = _userContext.CurrentUserID;

            var result = await _productionService.CreateIssueRequestAsync(NewIssueRequest);
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage(new { PlanID = NewIssueRequest.PlanID });
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadDataAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostIssueMaterialAsync()
        {
            if (ConfirmIssue.IssueID <= 0)
            {
                ErrorMessage = "Invalid Issue ID.";
                await LoadDataAsync();
                return Page();
            }

            ConfirmIssue.PerformedBy = _userContext.CurrentUserID;

            var result = await _productionService.IssueMaterialAsync(ConfirmIssue);
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage(new { PlanID });
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadDataAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostReturnMaterialAsync()
        {
            if (NewMaterialReturn.IssueID <= 0)
            {
                ErrorMessage = "Invalid Issue ID.";
                await LoadDataAsync();
                return Page();
            }

            NewMaterialReturn.ReturnedBy = _userContext.CurrentUserID;

            var result = await _productionService.ReturnMaterialAsync(NewMaterialReturn);
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage(new { PlanID });
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadDataAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnGetShelvesForProductAsync(int productId)
        {
            var shelves = await _productionService.GetShelvesWithProductAsync(productId);
            return new JsonResult(shelves);
        }

        private async Task LoadDataAsync()
        {
            try
            {
                if (TempData["SuccessMessage"] != null)
                {
                    SuccessMessage = TempData["SuccessMessage"]?.ToString() ?? "";
                }

                ActivePlansList = (await _productionService.GetActiveProductionPlansAsync()).ToList();
                RawMaterialsList = (await _productionService.GetRawMaterialsAsync()).ToList();
                ActiveShelvesList = (await _productionService.GetActiveShelvesAsync()).ToList();

                // Fetch live data from SQL
                var tasksData = (await _productionService.GetIssueReturnTasksAsync()).ToList();
                if (PlanID.HasValue)
                {
                    var planTasks = tasksData.Where(t => t.PlanID == PlanID.Value).ToList();
                    if (!planTasks.Any())
                    {
                        await _productionService.AutoGenerateMaterialIssuesAsync(PlanID.Value, _userContext.CurrentUserID);
                        tasksData = (await _productionService.GetIssueReturnTasksAsync()).ToList();
                        planTasks = tasksData.Where(t => t.PlanID == PlanID.Value).ToList();
                    }
                    tasksData = planTasks;
                    NewIssueRequest.PlanID = PlanID.Value;
                }
                var summaryData = await _productionService.GetIssueReturnSummaryAsync();

                if (tasksData.Any())
                {
                    // Map Current Plan Header (from first task for context)
                    var first = tasksData.First();
                    CurrentPlan = new ProductionPlanHeader
                    {
                        PlanNumber = first.PlanNumber,
                        FinishedProduct = first.ProductName, // Contextual FG
                        Location = first.WarehouseName,
                        ProductionLine = "Active Line"
                    };

                    // Map Picklist Items
                    IssueItems = tasksData.Select(t => new MaterialIssueRow
                    {
                        IssueID = t.IssueID,
                        SKU = t.SKU,
                        Name = t.ProductName,
                        Location = $"{t.WarehouseName} / {t.ShelfCode}",
                        ShelfID = t.ShelfID,
                        ReqQty = t.RequestedQty,
                        IssuedQty = t.IssuedQty,
                        ReturnedQty = t.ReturnedQty,
                        Status = t.Status,
                        StatusColor = GetStatusColor(t.Status, t.RequestedQty, t.AvailableQty),
                        Icon = GetItemIcon(t.ProductName),
                        IsActive = t.Status == "Pending" || t.Status == "Partially Issued"
                    }).ToList();

                    // Map Issued List (for return widget)
                    IssuedList = tasksData.Where(t => t.IssuedQty > t.ReturnedQty).Select(t => new IssuedMaterial
                    {
                        IssueID = t.IssueID,
                        SKU = t.SKU,
                        Name = t.ProductName,
                        IssuedQty = t.IssuedQty,
                        ReturnedQty = t.ReturnedQty,
                        DefaultShelfID = t.ShelfID
                    }).ToList();
                }
                else
                {
                    var selectedPlan = ActivePlansList.FirstOrDefault(p => PlanID.HasValue && p.ID == PlanID.Value)
                        ?? ActivePlansList.FirstOrDefault();
                    CurrentPlan = new ProductionPlanHeader
                    {
                        PlanNumber = selectedPlan?.Number ?? "No active plan",
                        FinishedProduct = "",
                        Location = PlanID.HasValue ? "No material requests yet" : "All active plans",
                        ProductionLine = "Issue materials from warehouse stock"
                    };
                }

                // Map Summary Cards
                var requestedQty = tasksData.Sum(x => x.RequestedQty);
                SummaryCards = new List<MIRKPICard>
                {
                    new MIRKPICard { Title = "Total Fulfillment", Value = requestedQty > 0 ? $"{(tasksData.Sum(x => x.IssuedQty) / requestedQty * 100):N0}%" : "0%", Trend = "", ColorClass = "primary" },
                    new MIRKPICard { Title = "Items Pending", Value = $"{tasksData.Count(t => t.Status == "Pending" || t.Status == "Partially Issued")} SKUs", Subtext = $"{tasksData.Sum(t => Math.Max(0, t.RequestedQty - t.AvailableQty)):N0} Shortage Units", ColorClass = "warning" },
                    new MIRKPICard { Title = "Issued Total", Value = $"{tasksData.Sum(t => t.IssuedQty):N0}", Subtext = "In Production", ColorClass = "success" },
                    new MIRKPICard { Title = "Returned Total", Value = $"{tasksData.Sum(t => t.ReturnedQty):N0}", Subtext = "Restocked", ColorClass = "secondary" }
                };
            }
            catch (Exception)
            {
                ErrorMessage = "Database connection failed. Showing offline mode.";
                IssueItems = new List<MaterialIssueRow>();
            }
        }

        private string GetStatusColor(string status, decimal req, decimal avail)
        {
            if (status == "Issued" || status == "Fulfilled") return "success";
            if (req > avail && (status == "Pending" || status == "Partially Issued")) return "danger";
            if (status == "Partially Issued") return "secondary";
            return "muted";
        }

        private string GetItemIcon(string name)
        {
            if (name.Contains("Motherboard")) return "settings_input_component";
            if (name.Contains("RAM")) return "memory";
            if (name.Contains("Power")) return "bolt";
            return "other_admission";
        }
    }

    public class ProductionPlanHeader
    {
        public string PlanNumber { get; set; } = string.Empty;
        public string FinishedProduct { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string ProductionLine { get; set; } = string.Empty;
    }

    public class MIRKPICard
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Trend { get; set; } = string.Empty;
        public string Subtext { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
    }

    public class MaterialIssueRow
    {
        public int IssueID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int ShelfID { get; set; }
        public decimal ReqQty { get; set; }
        public decimal IssuedQty { get; set; }
        public decimal ReturnedQty { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class IssuedMaterial
    {
        public int IssueID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal IssuedQty { get; set; }
        public decimal ReturnedQty { get; set; }
        public int DefaultShelfID { get; set; }
    }
}

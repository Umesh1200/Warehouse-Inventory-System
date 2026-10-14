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
    public class PutawayModel : PageModel
    {
        private readonly IPutawayService _putawayService;
        private readonly IUserContext _userContext;
        private readonly IUserRoleService _userRoleService;
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public PutawayModel(IPutawayService putawayService, IUserContext userContext, IUserRoleService userRoleService)
        {
            _putawayService = putawayService;
            _userContext = userContext;
            _userRoleService = userRoleService;
        }

        public List<PutawaySummaryCard> SummaryCards { get; set; } = new();
        public List<PutawayTaskRow> Tasks { get; set; } = new();
        public List<string> Warehouses { get; set; } = new();
        public List<string> ItemTypes { get; set; } = new();
        
        public List<UserDto> StaffUsers { get; set; } = new();

        public List<ShelfOptionDto> ActiveShelvesList { get; set; } = new();

        [BindProperty]
        public PutawayTaskCreateDto NewTask { get; set; } = new();

        [BindProperty]
        public PutawayCompleteDto CompleteTask { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        public async Task<IActionResult> OnPostCreateTaskAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return Page();
            }

            if ((User.IsInRole("Admin") || User.IsInRole("Manager")) && NewTask.AssignedTo.HasValue)
            {
                // Keep AssignedTo from dropdown
            }
            else
            {
                NewTask.AssignedTo = _userContext.CurrentUserID;
            }

            var result = await _putawayService.CreatePutawayTaskAsync(NewTask);
            
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

        public async Task<IActionResult> OnPostCompleteAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return Page();
            }

            CompleteTask.PerformedBy = _userContext.CurrentUserID;

            var result = await _putawayService.CompletePutawayAsync(CompleteTask);

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

        public async Task<IActionResult> OnGetSuggestedShelvesAsync(int productId, decimal quantity)
        {
            if (productId <= 0 || quantity <= 0)
            {
                return new JsonResult(new List<ShelfOptionDto>());
            }

            var shelves = await _putawayService.GetSuggestedShelvesAsync(productId, quantity);
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

                ItemTypes = new List<string> { "All Types", "Raw Material", "Finished Goods", "Consumable", "General" };

                // Load active shelves for dropdowns
                ActiveShelvesList = (await _putawayService.GetActiveShelvesAsync()).ToList();

                var userManagementData = await _userRoleService.GetUserManagementDataAsync();
                StaffUsers = userManagementData.Users.Where(u => u.IsActive && u.RoleName == "Warehouse Staff").ToList();

                // Fetch live data from SQL
                var putawayData = (await _putawayService.GetPutawayTasksAsync()).ToList();
                var summaryData = await _putawayService.GetPutawaySummaryAsync();

                Warehouses = new List<string> { "All Warehouses" };
                Warehouses.AddRange(putawayData.Where(t => !string.IsNullOrEmpty(t.WarehouseName)).Select(t => t.WarehouseName).Distinct());

                // Map Summary Cards
                SummaryCards = new List<PutawaySummaryCard>
                {
                    new PutawaySummaryCard { Title = "Pending Putaway", Value = summaryData.PendingPutaway.ToString(), Icon = "pending_actions", ColorClass = "warning" },
                    new PutawaySummaryCard { Title = "Completed Today", Value = summaryData.Completed.ToString(), Icon = "task_alt", ColorClass = "success" },
                    new PutawaySummaryCard { Title = "Waiting Units", Value = $"{summaryData.QtyWaitingPutaway:N0}", Icon = "fact_check", ColorClass = "info" },
                    new PutawaySummaryCard { Title = "Total Tasks", Value = summaryData.TotalTasks.ToString(), Icon = "assignment", ColorClass = "primary" }
                };

                // Map Putaway Rows
                Tasks = putawayData.Select(t => new PutawayTaskRow
                {
                    TaskID = t.TaskID,
                    QCID = t.QCID,
                    ProductID = t.ProductID,
                    TaskNo = t.TaskNo,
                    Reference = t.PONumber,
                    Product = t.ProductName,
                    SKU = t.SKU,
                    Batch = t.BatchNumber,
                    Qty = (int)t.QtyToPutaway,
                    ItemType = t.ProductType,
                    SourceType = t.SourceType,
                    SuggestedShelfID = t.SuggestedShelfID,
                    SuggestedShelf = string.IsNullOrEmpty(t.SuggestedShelfCode) ? "NOT ASSIGNED" : t.SuggestedShelfCode,
                    ActualShelf = string.IsNullOrEmpty(t.ActualShelfCode) ? "" : t.ActualShelfCode,
                    DisplayShelf = !string.IsNullOrEmpty(t.ActualShelfCode) ? t.ActualShelfCode : (string.IsNullOrEmpty(t.SuggestedShelfCode) ? "NOT ASSIGNED" : t.SuggestedShelfCode),
                    ShelfCapacity = t.SuggestedShelfID.HasValue ? $"{t.ShelfUtilizationPercentage:N0}% Full" : "-",
                    ShelfUtilization = t.ShelfUtilizationPercentage,
                    ShelfAvailableCapacity = t.ShelfAvailableCapacity,
                    ShelfWarning = t.ShelfUtilizationPercentage >= 80,
                    Warehouse = t.WarehouseName,
                    Floor = t.FloorName,
                    Room = t.RoomName,
                    Rack = t.RackName,
                    AssignedStaff = string.IsNullOrEmpty(t.AssignedToName) ? "Unassigned" : t.AssignedToName,
                    Status = t.Status,
                    StatusColor = GetStatusColor(t.Status),
                    CompletedDate = t.CompletedDate
                }).ToList();
            }
            catch (Exception)
            {
                ErrorMessage = "Database connection failed. Showing offline mode.";
                Tasks = new List<PutawayTaskRow>();
            }
        }

        private string GetStatusColor(string status)
        {
            return status switch
            {
                "Completed" => "success",
                "In Progress" => "info",
                "Pending" => "warning",
                "Pending Task" => "danger",
                _ => "muted"
            };
        }
    }

    public class PutawaySummaryCard
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
    }

    public class PutawayTaskRow
    {
        public int? TaskID { get; set; }
        public int QCID { get; set; }
        public int ProductID { get; set; }
        public string TaskNo { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string SourceType { get; set; } = "Purchase";
        public string Product { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string Batch { get; set; } = string.Empty;
        public int Qty { get; set; }
        public string ItemType { get; set; } = string.Empty;
        public int? SuggestedShelfID { get; set; }
        public string SuggestedShelf { get; set; } = string.Empty;
        public string ActualShelf { get; set; } = string.Empty;
        public string DisplayShelf { get; set; } = string.Empty;
        public string ShelfCapacity { get; set; } = string.Empty;
        public decimal ShelfUtilization { get; set; }
        public decimal ShelfAvailableCapacity { get; set; }
        public bool ShelfWarning { get; set; }
        public string Warehouse { get; set; } = string.Empty;
        public string Floor { get; set; } = string.Empty;
        public string Room { get; set; } = string.Empty;
        public string Rack { get; set; } = string.Empty;
        public string AssignedStaff { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public DateTime? CompletedDate { get; set; }
    }
}

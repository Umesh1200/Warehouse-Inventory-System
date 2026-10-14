using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using IMS.Services;
using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace IMS.Pages.Inventory
{
    public class CycleCountsModel : PageModel
    {
        private readonly ICycleCountService _cycleCountService;
        private readonly IWarehouseService _warehouseService;
        private readonly IUserRoleService _userRoleService;
        private readonly IProductService _productService;
        private readonly IUserContext _userContext;
        
        public string ErrorMessage { get; set; } = string.Empty;
        public bool IsAdminOrManager { get; set; }

        public CycleCountsModel(ICycleCountService cycleCountService, IWarehouseService warehouseService, IUserRoleService userRoleService, IProductService productService, IUserContext userContext)
        {
            _cycleCountService = cycleCountService;
            _warehouseService = warehouseService;
            _userRoleService = userRoleService;
            _productService = productService;
            _userContext = userContext;
        }

        public List<CycleCountSummaryCard> SummaryCards { get; set; } = new();
        public List<CycleCountRow> Tasks { get; set; } = new();
        public List<string> Warehouses { get; set; } = new();
        public List<string> CountTypes { get; set; } = new();
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> StaffList { get; set; } = new();

        [BindProperty]
        public CycleCountCreateDto NewCount { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                CountTypes = new List<string> { "All Types", "Blind Count", "Blind Double-Entry", "Spot Check", "Scheduled Audit" };
                
                Warehouses = new List<string> { "All Warehouses" };
                var warehouseData = await _warehouseService.GetWarehousesAsync();
                Warehouses.AddRange(warehouseData.Select(w => (string)w.Name));

                try {
                    var userData = await _userRoleService.GetUserManagementDataAsync();
                    StaffList = userData.Users
                        .Where(u => u.RoleName != null && (u.RoleName.Contains("Warehouse", StringComparison.OrdinalIgnoreCase) || u.RoleName.Contains("Staff", StringComparison.OrdinalIgnoreCase)))
                        .Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem {
                            Value = u.UserID.ToString(),
                            Text = $"{u.Username} - {u.FullName} ({u.RoleName})"
                        }).ToList();
                } catch {
                    // Fallback if users table offline
                    StaffList.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "1", Text = "Fallback User" });
                }

                IsAdminOrManager = _userContext.Role == "Admin" || _userContext.Role == "Manager";
                var countData = (await _cycleCountService.GetCycleCountsAsync()).ToList();
                
                if (!IsAdminOrManager)
                {
                    // Staff can only see their assigned counts
                    countData = countData.Where(c => c.AssignedTo == _userContext.CurrentUserID).ToList();
                }

                var summaryData = await _cycleCountService.GetCycleCountSummaryAsync();

                SummaryCards = new List<CycleCountSummaryCard>
                {
                    new CycleCountSummaryCard { Title = "Scheduled Counts", Value = summaryData.ScheduledCount.ToString(), Icon = "calendar_month", ColorClass = "primary" },
                    new CycleCountSummaryCard { Title = "In Progress", Value = summaryData.InProgressCount.ToString(), Icon = "hourglass_top", ColorClass = "info" },
                    new CycleCountSummaryCard { Title = "Completed Today", Value = summaryData.CompletedCount.ToString(), Icon = "task_alt", ColorClass = "success" },
                    new CycleCountSummaryCard { Title = "Variance Found", Value = summaryData.VarianceFoundCount.ToString(), Icon = "error", ColorClass = "danger" }
                };

                Tasks = countData.Select(c => new CycleCountRow
                {
                    CountID = c.CountID,
                    No = c.CountNo,
                    Product = c.ProductName, 
                    SKU = c.SKU,
                    Warehouse = "Warehouse", // Could get from join, but UI only asks for Location (Shelf)
                    Shelf = c.ShelfName,
                    SystemQty = c.SystemQty,
                    CountedQty = c.CountedQty ?? 0,
                    Type = c.CountType,
                    AssignedStaff = c.AssignedToName,
                    ScheduledDate = c.ScheduledDate?.ToString("MMM dd") ?? "TBD",
                    Status = c.Status,
                    StatusColor = GetStatusColor(c.Status)
                }).ToList();
            }
            catch (Exception)
            {
                ErrorMessage = "Database connection failed. Showing offline mode.";
            }
            return Page();
        }

        public async Task<IActionResult> OnPostCreateCountAsync()
        {
            var result = await _cycleCountService.CreateCycleCountAsync(NewCount, 1); // Mock user ID 1
            if (result.Success)
            {
                return RedirectToPage();
            }
            ErrorMessage = result.Message;
            return await OnGetAsync();
        }

        public async Task<IActionResult> OnPostStartCountAsync(int countId)
        {
            await _cycleCountService.StartCountAsync(countId, 1);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSubmitCountAsync(int countId, decimal countedQty)
        {
            await _cycleCountService.SubmitCountAsync(countId, countedQty, 1);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCreateAdjustmentAsync(int countId)
        {
            int userId = _userContext.CurrentUserID > 0 ? _userContext.CurrentUserID : 1;
            var result = await _cycleCountService.CreateAdjustmentAsync(countId, userId);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage("/Inventory/Adjustments");
            }
            ErrorMessage = result.Message;
            return await OnGetAsync();
        }

        public async Task<IActionResult> OnGetWarehousesAsync() => new JsonResult(await _warehouseService.GetWarehousesAsync());
        public async Task<IActionResult> OnGetFloorsAsync(int warehouseId) => new JsonResult(await _warehouseService.GetFloorsByWarehouseAsync(warehouseId));
        public async Task<IActionResult> OnGetShelvesAsync(int? floorId, int? warehouseId) 
        {
            var shelves = await _warehouseService.GetShelvesByFloorOrWarehouseAsync(floorId, warehouseId);
            return new JsonResult(shelves);
        }

        public async Task<IActionResult> OnGetProductsByLocationAsync(int? warehouseId, int? floorId, int? shelfId)
        {
            var products = (await _productService.GetProductsByLocationAsync(warehouseId, floorId, shelfId)).ToList();
            if (!products.Any() && !warehouseId.HasValue && !floorId.HasValue && !shelfId.HasValue)
            {
                try
                {
                    var allProducts = await _productService.GetProductMasterAsync();
                    if (allProducts != null && allProducts.Any())
                    {
                        return new JsonResult(allProducts.Select(p => new {
                            id = p.ProductID,
                            name = p.ProductName,
                            sku = p.SKU,
                            category = p.CategoryName,
                            unit = p.UnitCode
                        }));
                    }
                }
                catch { }
            }
            return new JsonResult(products);
        }

        private string GetStatusColor(string status)
        {
            return status switch
            {
                "Completed" => "success",
                "In Progress" => "info",
                "Scheduled" => "primary",
                "Variance Found" => "danger",
                _ => "muted"
            };
        }
    }

    public class CycleCountSummaryCard
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
    }

    public class CycleCountRow
    {
        public int CountID { get; set; }
        public string No { get; set; } = string.Empty;
        public string Product { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string Warehouse { get; set; } = string.Empty;
        public string Shelf { get; set; } = string.Empty;
        public decimal SystemQty { get; set; }
        public decimal CountedQty { get; set; }
        public decimal Variance => Status == "Scheduled" || Status == "In Progress" ? 0 : CountedQty - SystemQty;
        public string Type { get; set; } = string.Empty;
        public string AssignedStaff { get; set; } = string.Empty;
        public string ScheduledDate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
    }
}

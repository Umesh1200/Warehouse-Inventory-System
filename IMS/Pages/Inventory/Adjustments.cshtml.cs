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
    public class AdjustmentsModel : PageModel
    {
        private readonly IInventoryAdjustmentService _adjustmentService;
        private readonly IUserContext _userContext;
        public string ErrorMessage { get; set; } = string.Empty;
        public bool IsAdminOrManager { get; set; }

        public AdjustmentsModel(IInventoryAdjustmentService adjustmentService, IUserContext userContext)
        {
            _adjustmentService = adjustmentService;
            _userContext = userContext;
        }

        public List<AdjustmentSummaryCard> SummaryCards { get; set; } = new();
        public List<AdjustmentRow> Adjustments { get; set; } = new();
        public List<string> AdjustmentTypes { get; set; } = new();
        public List<string> Reasons { get; set; } = new();
        public List<string> WarehousesList { get; set; } = new();
        
        public List<dynamic> ProductsList { get; set; } = new();
        public List<dynamic> ShelvesList { get; set; } = new();

        [BindProperty]
        public AdjustmentCreateDto NewAdjustment { get; set; } = new();
        
        [BindProperty]
        public int NewItemProductID { get; set; }
        [BindProperty]
        public int NewItemShelfID { get; set; }
        [BindProperty]
        public decimal NewItemCountedQty { get; set; }

        [BindProperty]
        public int ApproveAdjustmentID { get; set; }

        [BindProperty]
        public int PostAdjustmentID { get; set; }

        public string SuccessMessage { get; set; } = string.Empty;

        public async Task OnGetAsync(int? productId)
        {
            if (productId.HasValue)
            {
                NewItemProductID = productId.Value;
            }
            await LoadDataAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (NewItemProductID <= 0)
            {
                ErrorMessage = "Please select a product.";
                await LoadDataAsync();
                return Page();
            }

            if (NewItemShelfID <= 0)
            {
                ErrorMessage = "Please select a shelf location.";
                await LoadDataAsync();
                return Page();
            }

            if (string.IsNullOrWhiteSpace(NewAdjustment.AdjustmentType))
            {
                ErrorMessage = "Please select an Adjustment Type.";
                await LoadDataAsync();
                return Page();
            }

            if (string.IsNullOrWhiteSpace(NewAdjustment.Reason))
            {
                ErrorMessage = "Please select a Reason.";
                await LoadDataAsync();
                return Page();
            }

            var systemQty = await _adjustmentService.GetCurrentStockAsync(NewItemProductID, NewItemShelfID);

            NewAdjustment.Items = new List<AdjustmentItemCreateDto>
            {
                new AdjustmentItemCreateDto
                {
                    ProductID = NewItemProductID,
                    ShelfID = NewItemShelfID,
                    SystemQty = systemQty,
                    CountedQty = NewItemCountedQty
                }
            };

            NewAdjustment.RequestedBy = _userContext.CurrentUserID > 0 ? _userContext.CurrentUserID : 1; 

            var result = await _adjustmentService.CreateAdjustmentAsync(NewAdjustment);
            
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
            IsAdminOrManager = _userContext.Role == "Admin" || _userContext.Role == "Manager";
            if (!IsAdminOrManager)
            {
                ErrorMessage = "Access Denied: Only Managers and Admins can approve adjustments.";
                await LoadDataAsync();
                return Page();
            }

            if (ApproveAdjustmentID <= 0)
            {
                ErrorMessage = "Invalid Adjustment ID.";
                await LoadDataAsync();
                return Page();
            }
            
            int approverId = _userContext.CurrentUserID > 0 ? _userContext.CurrentUserID : 1;
            var result = await _adjustmentService.ApproveAdjustmentAsync(ApproveAdjustmentID, approverId);
            
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

        public async Task<IActionResult> OnPostPostAdjustmentAsync()
        {
            IsAdminOrManager = _userContext.Role == "Admin" || _userContext.Role == "Manager";
            if (!IsAdminOrManager)
            {
                ErrorMessage = "Access Denied: Only Managers and Admins can post adjustments.";
                await LoadDataAsync();
                return Page();
            }

            if (PostAdjustmentID <= 0)
            {
                ErrorMessage = "Invalid Adjustment ID.";
                await LoadDataAsync();
                return Page();
            }
            
            int performerId = _userContext.CurrentUserID > 0 ? _userContext.CurrentUserID : 1;
            var result = await _adjustmentService.PostAdjustmentAsync(PostAdjustmentID, performerId);
            
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

        public async Task<IActionResult> OnGetShelvesForProductAsync(int productId)
        {
            var shelves = await _adjustmentService.GetShelvesWithProductAsync(productId);
            return new JsonResult(shelves);
        }

        public async Task<IActionResult> OnGetCurrentStockAsync(int productId, int shelfId)
        {
            var stock = await _adjustmentService.GetCurrentStockAsync(productId, shelfId);
            return new JsonResult(new { stock = stock });
        }

        public async Task<IActionResult> OnGetAdjustmentDetailsAsync(int id)
        {
            try
            {
                var adj = await _adjustmentService.GetAdjustmentByIdAsync(id);
                if (adj == null) return new JsonResult(new { success = false, message = "Adjustment not found." });

                var items = (await _adjustmentService.GetAdjustmentItemsAsync(id)).ToList();
                return new JsonResult(new { success = true, adjustment = adj, items = items });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                IsAdminOrManager = _userContext.Role == "Admin" || _userContext.Role == "Manager";

                if (TempData["SuccessMessage"] != null)
                {
                    SuccessMessage = TempData["SuccessMessage"]?.ToString() ?? "";
                }

                AdjustmentTypes = new List<string> { "All Types", "AdjustmentIn", "AdjustmentOut" };
                Reasons = new List<string> { "All Reasons", "Damaged", "Loss", "Counting Error", "System Mismatch", "Found in Stock", "Physical Audit Variance" };

                ProductsList = (await _adjustmentService.GetActiveProductsAsync()).ToList();
                ShelvesList = (await _adjustmentService.GetActiveShelvesAsync()).ToList();
                WarehousesList = (await _adjustmentService.GetWarehousesAsync()).ToList();

                var rows = (await _adjustmentService.GetAllAdjustmentRowsAsync()).ToList();
                var summaryData = await _adjustmentService.GetAdjustmentSummaryAsync();

                // Map Summary Cards
                SummaryCards = new List<AdjustmentSummaryCard>
                {
                    new AdjustmentSummaryCard { Title = "Pending Approval", Value = summaryData.AwaitingApprovalCount.ToString(), Icon = "pending_actions", ColorClass = "warning" },
                    new AdjustmentSummaryCard { Title = "Approved Total", Value = summaryData.ApprovedCount.ToString(), Icon = "check_circle", ColorClass = "success" },
                    new AdjustmentSummaryCard { Title = "Posted Total", Value = summaryData.PostedCount.ToString(), Icon = "publish", ColorClass = "info" },
                    new AdjustmentSummaryCard { Title = "Total Requests", Value = summaryData.TotalAdjustments.ToString(), Icon = "list_alt", ColorClass = "primary" }
                };

                // Map Adjustment Rows
                Adjustments = rows.Select(a => new AdjustmentRow
                {
                    AdjustmentID = a.AdjustmentID,
                    No = a.AdjustmentNo,
                    Product = string.IsNullOrWhiteSpace(a.ProductName) ? "Multiple Items" : a.ProductName,
                    SKU = string.IsNullOrWhiteSpace(a.SKU) || a.SKU == "-" ? $"{a.ItemCount} Item{(a.ItemCount == 1 ? "" : "s")}" : a.SKU,
                    Warehouse = string.IsNullOrWhiteSpace(a.WarehouseName) ? "Various" : a.WarehouseName,
                    Shelf = string.IsNullOrWhiteSpace(a.ShelfCode) ? "Various" : a.ShelfCode,
                    SystemQty = a.SystemQty,
                    CountedQty = a.CountedQty,
                    Type = a.AdjustmentType,
                    Reason = a.Reason,
                    RequestedBy = string.IsNullOrWhiteSpace(a.RequestedByName) ? "System User" : a.RequestedByName,
                    ApprovedBy = string.IsNullOrWhiteSpace(a.ApprovedByName) ? "Pending" : a.ApprovedByName,
                    Status = a.Status,
                    StatusColor = GetStatusColor(a.Status)
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in LoadDataAsync: " + ex.Message);
                ErrorMessage = "Failed to load adjustment records.";
            }
        }

        private string GetStatusColor(string status)
        {
            return status switch
            {
                "Posted" => "success",
                "Approved" => "info",
                "Pending" => "warning",
                "Awaiting Approval" => "warning",
                "Rejected" => "danger",
                _ => "muted"
            };
        }
    }

    public class AdjustmentSummaryCard
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
    }

    public class AdjustmentRow
    {
        public int AdjustmentID { get; set; }
        public string No { get; set; } = string.Empty;
        public string Product { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string Warehouse { get; set; } = string.Empty;
        public string Shelf { get; set; } = string.Empty;
        public decimal SystemQty { get; set; }
        public decimal CountedQty { get; set; }
        public decimal Difference => CountedQty - SystemQty;
        public string Type { get; set; } = string.Empty; // AdjustmentIn, AdjustmentOut
        public string Reason { get; set; } = string.Empty;
        public string RequestedBy { get; set; } = string.Empty;
        public string ApprovedBy { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
    }
}

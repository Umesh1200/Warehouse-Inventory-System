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
    public class TransfersModel : PageModel
    {
        private readonly IStockTransferService _transferService;
        private readonly IWarehouseService _warehouseService;
        private readonly IUserRoleService _userRoleService;
        private readonly IUserContext _userContext;
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public TransfersModel(
            IStockTransferService transferService, 
            IWarehouseService warehouseService, 
            IUserRoleService userRoleService,
            IUserContext userContext)
        {
            _transferService = transferService;
            _warehouseService = warehouseService;
            _userRoleService = userRoleService;
            _userContext = userContext;
        }

        public List<TransferSummaryCard> SummaryCards { get; set; } = new();
        public List<TransferRow> Transfers { get; set; } = new();
        public List<string> Warehouses { get; set; } = new();
        public List<string> TransferTypes { get; set; } = new();
        public List<UserDto> StaffUsers { get; set; } = new();
        
        public List<dynamic> ProductsList { get; set; } = new();
        public List<dynamic> ShelvesList { get; set; } = new();

        [BindProperty]
        public StockTransferCreateDto NewTransfer { get; set; } = new();

        [BindProperty]
        public int? AssignedToStaffID { get; set; }

        [BindProperty]
        public int NewItemProductID { get; set; }
        [BindProperty]
        public int NewItemFromShelfID { get; set; }
        [BindProperty]
        public int NewItemToShelfID { get; set; }
        [BindProperty]
        public decimal NewItemQuantity { get; set; }

        [BindProperty]
        public int ApproveTransferID { get; set; }

        [BindProperty]
        public int PostTransferID { get; set; }

        [BindProperty]
        public int CancelTransferID { get; set; }

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
            if (NewItemProductID > 0 && NewItemFromShelfID > 0 && NewItemToShelfID > 0)
            {
                NewTransfer.Items.Add(new StockTransferItemCreateDto
                {
                    ProductID = NewItemProductID,
                    FromShelfID = NewItemFromShelfID,
                    ToShelfID = NewItemToShelfID,
                    Quantity = NewItemQuantity
                });
            }
            
            if (string.IsNullOrEmpty(NewTransfer.TransferType))
            {
                NewTransfer.TransferType = "Inter-Shelf";
            }

            NewTransfer.RequestedBy = _userContext.CurrentUserID;
            NewTransfer.AssignedTo = AssignedToStaffID;

            var result = await _transferService.CreateTransferAsync(NewTransfer);
            
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
            if (ApproveTransferID <= 0)
            {
                ErrorMessage = "Invalid Transfer ID.";
                await LoadDataAsync();
                return Page();
            }
            
            var result = await _transferService.ApproveTransferAsync(ApproveTransferID, _userContext.CurrentUserID);
            
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
            if (CancelTransferID <= 0)
            {
                ErrorMessage = "Invalid Transfer ID.";
                await LoadDataAsync();
                return Page();
            }

            var result = await _transferService.CancelTransferAsync(CancelTransferID, _userContext.CurrentUserID);

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

        public async Task<IActionResult> OnPostPostTransferAsync()
        {
            if (PostTransferID <= 0)
            {
                ErrorMessage = "Invalid Transfer ID.";
                await LoadDataAsync();
                return Page();
            }
            
            var result = await _transferService.PostTransferAsync(PostTransferID, _userContext.CurrentUserID);
            
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
            var shelves = await _transferService.GetShelvesWithProductAsync(productId);
            return new JsonResult(shelves);
        }

        public async Task<IActionResult> OnGetTransferItemsAsync(int transferId)
        {
            var items = await _transferService.GetTransferItemsAsync(transferId);
            return new JsonResult(items);
        }

        public async Task<IActionResult> OnGetWarehousesAsync() => new JsonResult(await _warehouseService.GetWarehousesAsync());
        public async Task<IActionResult> OnGetFloorsAsync(int warehouseId) => new JsonResult(await _warehouseService.GetFloorsByWarehouseAsync(warehouseId));
        public async Task<IActionResult> OnGetRoomsAsync(int floorId) => new JsonResult(await _warehouseService.GetRoomsByFloorAsync(floorId));
        public async Task<IActionResult> OnGetRacksAsync(int roomId) => new JsonResult(await _warehouseService.GetRacksByRoomAsync(roomId));
        public async Task<IActionResult> OnGetShelvesAsync(int rackId) => new JsonResult(await _warehouseService.GetShelvesByRackAsync(rackId));
        public async Task<IActionResult> OnGetShelfProductsAsync(int shelfId) => new JsonResult(await _transferService.GetProductsByShelfAsync(shelfId));
        public async Task<IActionResult> OnGetDestinationShelvesAsync(int productId, decimal quantity)
        {
            if (productId <= 0 || quantity <= 0)
            {
                return new JsonResult(new List<ShelfOptionDto>());
            }

            return new JsonResult(await _transferService.GetDestinationShelvesForProductAsync(productId, quantity));
        }
        public async Task<IActionResult> OnGetShelfCapacityDetailsAsync(int shelfId, int? productId = null)
        {
            var details = await _transferService.GetShelfCapacityInfoAsync(shelfId, productId);
            return new JsonResult(details);
        }

        private async Task LoadDataAsync()
        {
            try
            {
                if (TempData["SuccessMessage"] != null)
                {
                    SuccessMessage = TempData["SuccessMessage"]?.ToString() ?? "";
                }

                Warehouses = new List<string> { "All Warehouses" };
                var warehouseData = await _warehouseService.GetWarehousesAsync();
                Warehouses.AddRange(warehouseData.Select(w => (string)w.Name));
                TransferTypes = new List<string> { "Inter-Shelf", "Inter-Rack", "Inter-Room", "Inter-Floor", "Inter-Warehouse" };

                ProductsList = (await _transferService.GetActiveProductsAsync()).ToList();
                ShelvesList = (await _transferService.GetActiveShelvesAsync()).ToList();

                try {
                    var userData = await _userRoleService.GetUserManagementDataAsync();
                    StaffUsers = userData.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToList();
                } catch {
                    StaffUsers = new List<UserDto>();
                }

                // Fetch live data from SQL
                var transferData = (await _transferService.GetTransfersAsync()).ToList();
                var summaryData = await _transferService.GetTransferSummaryAsync();

                // Map Summary Cards
                SummaryCards = new List<TransferSummaryCard>
                {
                    new TransferSummaryCard { Title = "Pending Transfers", Value = summaryData.PendingTransfers.ToString(), Icon = "pending_actions", ColorClass = "warning" },
                    new TransferSummaryCard { Title = "Awaiting Approval", Value = summaryData.AwaitingApprovalCount.ToString(), Icon = "rate_review", ColorClass = "info" },
                    new TransferSummaryCard { Title = "In Transit", Value = summaryData.InTransitCount.ToString(), Icon = "local_shipping", ColorClass = "info" },
                    new TransferSummaryCard { Title = "Completed Today", Value = summaryData.CompletedCount.ToString(), Icon = "task_alt", ColorClass = "success" }
                };

                // Map Transfer Rows with live locations, shelf codes, and assigned staff
                Transfers = transferData.Select(t => new TransferRow
                {
                    TransferID = t.TransferID,
                    TransferNo = t.TransferNo,
                    Product = !string.IsNullOrEmpty(t.FirstProductName) 
                        ? (t.ItemCount > 1 ? $"{t.FirstProductName} (+{t.ItemCount - 1} more)" : t.FirstProductName) 
                        : "No Items",
                    SKU = !string.IsNullOrEmpty(t.FirstSKU) 
                        ? (t.ItemCount > 1 ? $"{t.FirstSKU} (+{t.ItemCount - 1})" : t.FirstSKU) 
                        : $"{t.ItemCount} SKUs",
                    Qty = (int)t.TotalQuantity,
                    FromLocation = !string.IsNullOrEmpty(t.FromWarehouseName) ? t.FromWarehouseName : "Various",
                    FromShelfCode = t.FromShelfCode,
                    ToLocation = !string.IsNullOrEmpty(t.ToWarehouseName) ? t.ToWarehouseName : "Various",
                    ToShelfCode = t.ToShelfCode,
                    Type = t.TransferType,
                    RequestedBy = t.RequestedByName,
                    ApprovedBy = t.ApprovedByName,
                    AssignedTo = t.AssignedTo,
                    AssignedToName = t.AssignedToName,
                    Status = t.Status,
                    StatusColor = GetStatusColor(t.Status)
                }).ToList();
            }
            catch (Exception)
            {
                ErrorMessage = "Database connection failed. Showing offline mode.";
                Transfers = new List<TransferRow>();
            }
        }

        private string GetStatusColor(string status)
        {
            return status switch
            {
                "Completed" => "success",
                "In Transit" => "info",
                "Pending" => "warning",
                "Awaiting Approval" => "warning",
                "Approved" => "info",
                "Cancelled" => "danger",
                _ => "muted"
            };
        }
    }

    public class TransferSummaryCard
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
    }

    public class TransferRow
    {
        public int TransferID { get; set; }
        public string TransferNo { get; set; } = string.Empty;
        public string Product { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int Qty { get; set; }
        public string FromLocation { get; set; } = string.Empty;
        public string FromShelfCode { get; set; } = string.Empty;
        public string ToLocation { get; set; } = string.Empty;
        public string ToShelfCode { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; 
        public string RequestedBy { get; set; } = string.Empty;
        public string ApprovedBy { get; set; } = string.Empty;
        public int? AssignedTo { get; set; }
        public string AssignedToName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
    }
}

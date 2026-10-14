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
    public class PickingPackingModel : PageModel
    {
        private readonly IPickingPackingService _pickPackService;
        private readonly IUserContext _userContext;
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public PickingPackingModel(IPickingPackingService pickPackService, IUserContext userContext)
        {
            _pickPackService = pickPackService;
            _userContext = userContext;
        }

        public List<PickPackKPICard> SummaryCards { get; set; } = new();
        public List<PickPackTaskRow> OrdersQueue { get; set; } = new();
        public PickPackTaskRow SelectedOrder { get; set; } = new();
        public List<PickItemAllocation> SelectedOrderItems { get; set; } = new();

        public List<dynamic> StaffList { get; set; } = new();
        public List<dynamic> ShelvesList { get; set; } = new();
        public Dictionary<int, List<ShelfSelectOption>> ProductShelves { get; set; } = new();

        [BindProperty]
        public CreatePickTaskDto NewPickTask { get; set; } = new();
        [BindProperty]
        public int NewItemProductID { get; set; }
        [BindProperty]
        public int NewItemShelfID { get; set; }
        [BindProperty]
        public decimal NewItemQty { get; set; }

        [BindProperty]
        public int StartPickID { get; set; }

        [BindProperty]
        public int MarkPickedID { get; set; }

        [BindProperty]
        public PackingCreateDto NewPacking { get; set; } = new();

        public async Task OnGetAsync(int? soid)
        {
            await LoadDataAsync(soid);
        }

        public async Task<IActionResult> OnPostCreatePickTaskAsync()
        {
            if ((NewPickTask.Items == null || !NewPickTask.Items.Any()) && NewItemProductID > 0 && NewItemShelfID > 0 && NewItemQty > 0)
            {
                NewPickTask.Items ??= new List<PickItemReservationDto>();
                NewPickTask.Items.Add(new PickItemReservationDto
                {
                    ProductID = NewItemProductID,
                    ShelfID = NewItemShelfID,
                    QtyToReserve = NewItemQty
                });
            }

            var result = await _pickPackService.CreatePickTaskAsync(NewPickTask);
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage();
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadDataAsync(null);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostStartPickingAsync()
        {
            if (StartPickID <= 0)
            {
                ErrorMessage = "Invalid Pick ID.";
                await LoadDataAsync(null);
                return Page();
            }

            var result = await _pickPackService.StartPickingAsync(StartPickID, _userContext.CurrentUserID);
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage();
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadDataAsync(null);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostMarkPickedAsync()
        {
            if (MarkPickedID <= 0)
            {
                ErrorMessage = "Invalid Pick ID.";
                await LoadDataAsync(null);
                return Page();
            }

            var result = await _pickPackService.MarkPickedAsync(MarkPickedID, _userContext.CurrentUserID);
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage();
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadDataAsync(null);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostCompletePackingAsync()
        {
            NewPacking.PackedBy = _userContext.CurrentUserID;

            var result = await _pickPackService.CompletePackingAsync(NewPacking);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage();
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadDataAsync(null);
                return Page();
            }
        }

        private async Task LoadDataAsync(int? soid)
        {
            try
            {
                if (TempData["SuccessMessage"] != null)
                {
                    SuccessMessage = TempData["SuccessMessage"]?.ToString() ?? "";
                }

                StaffList = (await _pickPackService.GetActiveWarehouseStaffAsync()).ToList();
                ShelvesList = (await _pickPackService.GetActiveShelvesAsync()).ToList();

                // Fetch live data from SQL
                var pickPackData = (await _pickPackService.GetPickPackTasksAsync()).ToList();
                var confirmedOrders = (await _pickPackService.GetConfirmedSalesOrdersAsync()).ToList();
                var summaryData = await _pickPackService.GetPickPackSummaryAsync();

                // Map Summary Cards
                SummaryCards = new List<PickPackKPICard>
                {
                    new PickPackKPICard { Title = "Pending Picking", Value = summaryData.PendingPicking.ToString(), Icon = "pending_actions", ColorClass = "warning" },
                    new PickPackKPICard { Title = "Picking in Progress", Value = summaryData.PickingInProgress.ToString(), Icon = "directions_run", ColorClass = "info" },
                    new PickPackKPICard { Title = "Picked Orders", Value = summaryData.PickedOrders.ToString(), Icon = "inventory_2", ColorClass = "success" },
                    new PickPackKPICard { Title = "Packed Today", Value = summaryData.PackedOrders.ToString(), Icon = "package_2", ColorClass = "primary" }
                };

                // Map Orders Queue (Active Tasks)
                OrdersQueue = pickPackData.Select(t => new PickPackTaskRow
                {
                    PickID = t.PickID,
                    SOID = t.SOID,
                    SONumber = t.SONumber,
                    Customer = t.CustomerName,
                    ItemCount = t.ItemCount,
                    TotalUnits = (int)t.TotalOrderedQty,
                    Status = t.PickStatus,
                    PickStatusColor = GetPickStatusColor(t.PickStatus),
                    PackStatus = t.PackStatus ?? "Not Started",
                    PackStatusColor = GetPackStatusColor(t.PackStatus ?? ""),
                    IsSelected = soid.HasValue ? t.SOID == soid.Value : false
                }).ToList();

                // Add waiting orders to queue if they aren't already there
                foreach (var co in confirmedOrders)
                {
                    if (!OrdersQueue.Any(q => q.SOID == co.ID))
                    {
                        OrdersQueue.Add(new PickPackTaskRow
                        {
                            SOID = co.ID,
                            SONumber = co.Number,
                            Customer = "Waiting Allocation",
                            Status = "Confirmed",
                            PickStatusColor = "muted",
                            PackStatus = "Not Started",
                            PackStatusColor = "muted",
                            IsSelected = soid.HasValue ? co.ID == soid.Value : false
                        });
                    }
                }

                if (OrdersQueue.Any())
                {
                    if (!soid.HasValue) 
                    {
                        OrdersQueue.First().IsSelected = true;
                        soid = OrdersQueue.First().SOID;
                    }
                    
                    SelectedOrder = OrdersQueue.FirstOrDefault(q => q.SOID == soid.Value) ?? OrdersQueue.First();
                    
                    // Fetch items for details
                    var items = await _pickPackService.GetTaskItemsAsync(soid.Value);
                    SelectedOrderItems = items.Select(i => new PickItemAllocation
                    {
                        ProductID = i.ProductID,
                        SKU = i.SKU,
                        ProductName = i.ProductName,
                        QtyOrdered = (int)i.OrderedQty,
                        QtyReserved = (int)i.ReservedQty,
                        QtyAvailable = (int)i.AvailableQty,
                        Location = i.ShelfCode,
                        Status = i.ReservedQty >= i.OrderedQty ? "Allocated" : "Shortage",
                        Severity = i.ReservedQty >= i.OrderedQty ? "success" : "danger"
                    }).ToList();

                    ProductShelves = new Dictionary<int, List<ShelfSelectOption>>();
                    foreach (var item in SelectedOrderItems)
                    {
                        ProductShelves[item.ProductID] = (await _pickPackService.GetShelvesWithProductAsync(item.ProductID))
                            .Select(s => new ShelfSelectOption
                            {
                                ID = s.ID,
                                Name = s.Name
                            })
                            .ToList();
                    }

                    if (SelectedOrderItems.Any())
                    {
                        ShelvesList = ProductShelves.TryGetValue(SelectedOrderItems.First().ProductID, out var firstShelves)
                            ? firstShelves.Cast<dynamic>().ToList()
                            : new List<dynamic>();
                    }
                }
            }
            catch (Exception)
            {
                ErrorMessage = "Database connection failed. Showing offline mode.";
                OrdersQueue = new List<PickPackTaskRow>();
            }
        }

        private string GetPickStatusColor(string status)
        {
            return status switch
            {
                "Picked" => "success",
                "Picking" => "info",
                "Pending" => "warning",
                "Confirmed" => "muted",
                _ => "muted"
            };
        }

        private string GetPackStatusColor(string status)
        {
            return status switch
            {
                "Packed" => "success",
                "In Progress" => "info",
                _ => "muted"
            };
        }
    }

    public class PickPackKPICard
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
    }

    public class PickPackTaskRow
    {
        public int PickID { get; set; }
        public int SOID { get; set; }
        public string SONumber { get; set; } = string.Empty;
        public string Customer { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public int TotalUnits { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PickStatusColor { get; set; } = string.Empty;
        public string PackStatus { get; set; } = string.Empty;
        public string PackStatusColor { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
        public string Priority { get; set; } = "Standard";
    }

    public class PickItemAllocation
    {
        public int ProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int QtyOrdered { get; set; }
        public int QtyReserved { get; set; }
        public int QtyAvailable { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
    }

    public class ShelfSelectOption
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}

using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using IMS.Services;
using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace IMS.Pages.Sales
{
    public class OrdersModel : PageModel
    {
        private readonly ISalesOrderService _soService;
        private readonly IUserContext _userContext;
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public OrdersModel(ISalesOrderService soService, IUserContext userContext)
        {
            _soService = soService;
            _userContext = userContext;
        }

        public List<KPICard> SummaryCards { get; set; } = new();
        public List<SalesOrderRow> SalesOrders { get; set; } = new();
        public SalesOrderRow SelectedOrder { get; set; } = new();
        public List<LineItemAllocation> SelectedOrderItems { get; set; } = new();

        public List<dynamic> ActiveCustomersList { get; set; } = new();
        public List<dynamic> ActiveProductsList { get; set; } = new();

        [BindProperty]
        public SalesOrderCreateDto NewSalesOrder { get; set; } = new();

        [BindProperty]
        public int NewItemProductID { get; set; }
        [BindProperty]
        public decimal NewItemOrderedQty { get; set; }
        [BindProperty]
        public decimal NewItemUnitPrice { get; set; }

        [BindProperty]
        public SalesOrderUpdateDto EditSalesOrder { get; set; } = new();

        [BindProperty]
        public int EditItemProductID { get; set; }
        [BindProperty]
        public decimal EditItemOrderedQty { get; set; }
        [BindProperty]
        public decimal EditItemUnitPrice { get; set; }

        [BindProperty]
        public int ConfirmSOID { get; set; }

        [BindProperty]
        public int CancelSOID { get; set; }

        public async Task OnGetAsync(int? soid)
        {
            await LoadDataAsync(soid);
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (NewItemProductID > 0 && NewItemOrderedQty > 0)
            {
                NewSalesOrder.Items.Add(new SalesOrderItemCreateDto
                {
                    ProductID = NewItemProductID,
                    OrderedQty = NewItemOrderedQty,
                    UnitPrice = NewItemUnitPrice
                });
            }

            NewSalesOrder.CreatedBy = _userContext.CurrentUserID;

            var result = await _soService.CreateSalesOrderAsync(NewSalesOrder);
            
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

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            if (EditItemProductID > 0 && EditItemOrderedQty > 0)
            {
                EditSalesOrder.Items.Add(new SalesOrderItemCreateDto
                {
                    ProductID = EditItemProductID,
                    OrderedQty = EditItemOrderedQty,
                    UnitPrice = EditItemUnitPrice
                });
            }

            var result = await _soService.UpdateSalesOrderAsync(EditSalesOrder);

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

        public async Task<IActionResult> OnPostConfirmAsync()
        {
            if (ConfirmSOID <= 0)
            {
                ErrorMessage = "Invalid SO ID.";
                await LoadDataAsync(null);
                return Page();
            }
            
            var result = await _soService.ConfirmSalesOrderAsync(ConfirmSOID);
            
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

        public async Task<IActionResult> OnPostCancelAsync()
        {
            if (CancelSOID <= 0)
            {
                ErrorMessage = "Invalid SO ID.";
                await LoadDataAsync(null);
                return Page();
            }
            
            var result = await _soService.CancelSalesOrderAsync(CancelSOID);
            
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

                ActiveCustomersList = (await _soService.GetActiveCustomersAsync()).ToList();
                ActiveProductsList = (await _soService.GetActiveProductsAsync()).ToList();

                // Fetch live data from SQL
                var soData = (await _soService.GetSalesOrdersAsync()).ToList();
                var summaryData = await _soService.GetSalesOrderSummaryAsync();

                var today = DateTime.Today;
                var lastSevenDays = Enumerable.Range(6, 7)
                    .Select(offset => today.AddDays(-offset))
                    .ToList();

                SummaryCards = new List<KPICard>
                {
                    BuildKpiCard("Total Sales Orders", summaryData.TotalOrders, "receipt_long", "primary", soData, lastSevenDays, _ => true),
                    BuildKpiCard("Confirmed Orders", summaryData.ConfirmedOrders, "check_circle", "info", soData, lastSevenDays, o => o.Status == "Confirmed"),
                    BuildKpiCard("Ready to Pick", summaryData.PickingOrders, "inventory_2", "warning", soData, lastSevenDays, o => new[] { "ReadyToPick", "Allocated", "Picking" }.Contains(o.Status)),
                    BuildKpiCard("Dispatch Pending", summaryData.DispatchPendingOrders, "local_shipping", "success", soData, lastSevenDays, o => new[] { "Packed", "Dispatched" }.Contains(o.Status))
                };

                // Map Sales Order Rows
                SalesOrders = soData.Select(o => new SalesOrderRow
                {
                    SOID = o.SOID,
                    CustomerID = o.CustomerID,
                    RawOrderDate = o.OrderDate.ToString("yyyy-MM-dd"),
                    RawRequiredDate = o.RequiredDate?.ToString("yyyy-MM-dd") ?? "",
                    Remarks = o.Remarks,
                    SONumber = o.SONumber,
                    Customer = o.CustomerName,
                    OrderDate = o.OrderDate.ToString("MMM dd, yyyy"),
                    RequiredDate = o.RequiredDate?.ToString("MMM dd, yyyy") ?? "TBD",
                    TotalAmount = $"Rs. {o.TotalAmount:N2}",
                    ItemCount = o.ItemCount,
                    TotalUnits = o.TotalOrderedQty,
                    OrderStatus = o.Status,
                    ReservationStatus = GetReservationStatus(o),
                    IsSelected = soid.HasValue && o.SOID == soid.Value
                }).ToList();

                if (SalesOrders.Any())
                {
                    if (!SalesOrders.Any(order => order.IsSelected))
                    {
                        SalesOrders.First().IsSelected = true;
                    }

                    SelectedOrder = SalesOrders.First(order => order.IsSelected);
                    var selectedItems = (await _soService.GetSalesOrderItemsAsync(SelectedOrder.SOID)).ToList();
                    SelectedOrderItems = new List<LineItemAllocation>();

                    foreach (var item in selectedItems)
                    {
                        var available = await _soService.GetAvailableStockForProductAsync(item.ProductID);
                        var severity = available >= item.OrderedQty ? "success" : available > 0 ? "warning" : "danger";
                        var stockLocs = (await _soService.GetProductStockLocationsAsync(item.ProductID)).ToList();

                        SelectedOrderItems.Add(new LineItemAllocation
                        {
                            ProductID = item.ProductID,
                            UnitPrice = item.UnitPrice,
                            SKU = item.SKU,
                            ProductName = item.ProductName,
                            QtyOrdered = (int)item.OrderedQty,
                            QtyAvailable = (int)available,
                            Price = $"Rs. {item.TotalPrice:N2}",
                            Status = severity == "success" ? "Available" : severity == "warning" ? "Partial stock" : "Shortage",
                            Severity = severity,
                            Note = item.DispatchedQty > 0 ? $"{item.DispatchedQty:0.##} unit(s) already dispatched" : "",
                            Locations = stockLocs
                        });
                    }
                }
            }
            catch (Exception)
            {
                ErrorMessage = "Database connection failed. Showing offline mode.";
                SalesOrders = new List<SalesOrderRow>();
            }
        }

        private string GetReservationStatus(SalesOrderDto order)
        {
            if (order.Status == "Fulfilled" || order.Status == "Dispatched") return "Consumed";
            if (order.Status == "Allocated" || order.Status == "ReadyToPick") return "Full";
            if (order.Status == "Confirmed") return "Partial";
            return "None";
        }

        private KPICard BuildKpiCard(string title, int value, string icon, string colorClass, List<SalesOrderDto> orders, List<DateTime> days, Func<SalesOrderDto, bool> predicate)
        {
            var dailyValues = days
                .Select(day => orders.Count(order => order.OrderDate.Date == day.Date && predicate(order)))
                .ToList();

            var max = Math.Max(1, dailyValues.Max());
            var points = dailyValues.Select((count, index) =>
            {
                var x = index * 16;
                var y = 36 - ((decimal)count / max * 30);
                return $"{x:0.##},{y:0.##}";
            });

            return new KPICard
            {
                Title = title,
                Value = value.ToString("N0"),
                Icon = icon,
                ColorClass = colorClass,
                TrendPoints = string.Join(" ", points),
                TrendLabel = $"{dailyValues.Sum():N0} in last 7 days"
            };
        }
    }

    public class KPICard
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
        public string TrendPoints { get; set; } = string.Empty;
        public string TrendLabel { get; set; } = string.Empty;
    }

    public class SalesOrderRow
    {
        public int SOID { get; set; }
        public int CustomerID { get; set; }
        public string RawOrderDate { get; set; } = string.Empty;
        public string RawRequiredDate { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;

        public string SONumber { get; set; } = string.Empty;
        public string Customer { get; set; } = string.Empty;
        public string OrderDate { get; set; } = string.Empty;
        public string RequiredDate { get; set; } = string.Empty;
        public string TotalAmount { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public decimal TotalUnits { get; set; }
        public string ReservationStatus { get; set; } = string.Empty;
        public string OrderStatus { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    public class LineItemAllocation
    {
        public int ProductID { get; set; }
        public decimal UnitPrice { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int QtyOrdered { get; set; }
        public int QtyAvailable { get; set; }
        public string Price { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public List<InventoryOverviewDto> Locations { get; set; } = new();
    }
}

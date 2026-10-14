using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using IMS.Services;
using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace IMS.Pages.SupplierPortal
{
    public class PurchaseOrdersModel : PageModel
    {
        private readonly IPurchaseOrderService _poService;
        private readonly IUserContext _userContext;
        private readonly string _connectionString;

        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public List<POSummaryCard> SummaryCards { get; set; } = new();
        public List<PurchaseOrderRow> PurchaseOrders { get; set; } = new();
        public PurchaseOrderRow? SelectedPurchaseOrder { get; set; }
        public List<PurchaseOrderItemRow> SelectedItems { get; set; } = new();

        [BindProperty]
        public int ShipPOID { get; set; }

        [BindProperty]
        public string ShipCarrier { get; set; } = string.Empty;

        [BindProperty]
        public string ShipTrackingNumber { get; set; } = string.Empty;

        [BindProperty]
        public DateTime ShipExpectedDate { get; set; } = DateTime.Today.AddDays(3);

        [BindProperty]
        public List<ShippedQtyInput> ShippedQuantities { get; set; } = new();

        [BindProperty]
        public int RejectPOID { get; set; }

        [BindProperty]
        public string RejectReason { get; set; } = string.Empty;

        public PurchaseOrdersModel(IPurchaseOrderService poService, IUserContext userContext, IConfiguration configuration)
        {
            _poService = poService;
            _userContext = userContext;
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("Connection string not found.");
        }

        public async Task OnGetAsync(int? poid)
        {
            await LoadDataAsync(poid);
        }

        public async Task<IActionResult> OnPostMarkAsShippedAsync()
        {
            if (!_userContext.SupplierID.HasValue)
            {
                return RedirectToPage("/Account/AccessDenied");
            }

            if (ShipPOID <= 0)
            {
                ErrorMessage = "Invalid Purchase Order ID.";
                await LoadDataAsync(null);
                return Page();
            }

            try
            {
                var itemQuantities = ShippedQuantities.ToDictionary(x => x.POItemID, x => x.Quantity);

                var result = await _poService.ShipPurchaseOrderAsync(
                    ShipPOID, 
                    _userContext.SupplierID.Value, 
                    ShipCarrier, 
                    ShipTrackingNumber, 
                    ShipExpectedDate, 
                    itemQuantities);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToPage(new { poid = ShipPOID });
                }
                else
                {
                    ErrorMessage = result.Message;
                    await LoadDataAsync(ShipPOID);
                    return Page();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to update PO status: {ex.Message}";
                await LoadDataAsync(ShipPOID);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostRejectPOAsync()
        {
            if (!_userContext.SupplierID.HasValue)
            {
                return RedirectToPage("/Account/AccessDenied");
            }

            if (RejectPOID <= 0 || string.IsNullOrWhiteSpace(RejectReason))
            {
                ErrorMessage = "Purchase Order ID and rejection reason are required.";
                await LoadDataAsync(null);
                return Page();
            }

            try
            {
                var result = await _poService.RejectPurchaseOrderAsync(RejectPOID, _userContext.SupplierID.Value, RejectReason);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToPage(new { poid = RejectPOID });
                }
                else
                {
                    ErrorMessage = result.Message;
                    await LoadDataAsync(RejectPOID);
                    return Page();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to reject PO: {ex.Message}";
                await LoadDataAsync(RejectPOID);
                return Page();
            }
        }

        private async Task LoadDataAsync(int? selectedPoid)
        {
            try
            {
                if (TempData["SuccessMessage"] != null)
                {
                    SuccessMessage = TempData["SuccessMessage"]?.ToString() ?? "";
                }

                if (!_userContext.SupplierID.HasValue) return;
                int supplierId = _userContext.SupplierID.Value;

                // Load POs list (automatically filtered by SupplierID in PO Service)
                var posList = (await _poService.GetPurchaseOrdersAsync()).ToList();

                // Compute summary metrics
                int totalCount = posList.Count;
                int confirmedCount = posList.Count(o => o.Status == "Confirmed");
                int inTransitCount = posList.Count(o => o.Status == "InTransit");
                decimal completedValue = posList.Where(o => o.Status == "Closed" || o.Status == "Received").Sum(o => o.TotalAmount);

                SummaryCards = new List<POSummaryCard>
                {
                    new POSummaryCard { Title = "Total Orders", Value = totalCount.ToString(), Icon = "receipt_long", ColorClass = "primary" },
                    new POSummaryCard { Title = "Confirmed Orders", Value = confirmedCount.ToString(), Icon = "check_circle", ColorClass = "info" },
                    new POSummaryCard { Title = "In Transit", Value = inTransitCount.ToString(), Icon = "local_shipping", ColorClass = "warning" },
                    new POSummaryCard { Title = "Completed Value", Value = $"Rs. {completedValue:N2}", Icon = "payments", ColorClass = "success" }
                };

                PurchaseOrders = posList.Select(o => new PurchaseOrderRow
                {
                    POID = o.POID,
                    PONumber = o.PONumber,
                    OrderDate = o.OrderDate.ToString("MMM dd, yyyy"),
                    ExpectedDate = o.ExpectedDate?.ToString("MMM dd, yyyy") ?? "Not Set",
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    Remarks = o.Remarks,
                    Carrier = o.Carrier,
                    TrackingNumber = o.TrackingNumber,
                    RejectionReason = o.RejectionReason,
                    ItemCount = o.ItemCount,
                    IsSelected = selectedPoid.HasValue && o.POID == selectedPoid.Value
                }).ToList();

                // Handle selected PO details
                SelectedPurchaseOrder = PurchaseOrders.FirstOrDefault(o => o.IsSelected) ?? PurchaseOrders.FirstOrDefault();
                if (SelectedPurchaseOrder != null)
                {
                    SelectedPurchaseOrder.IsSelected = true;
                    var items = await _poService.GetPurchaseOrderItemsAsync(SelectedPurchaseOrder.POID);
                    SelectedItems = items.Select(i => new PurchaseOrderItemRow
                    {
                        POItemID = i.POItemID,
                        ProductID = i.ProductID,
                        SKU = i.SKU,
                        ProductName = i.ProductName,
                        OrderedQty = i.OrderedQty,
                        ReceivedQty = i.ReceivedQty,
                        ShippedQty = i.ShippedQty,
                        UnitPrice = i.UnitPrice,
                        TotalPrice = i.TotalPrice
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load PO data: {ex.Message}";
            }
        }
    }

    public class POSummaryCard
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
    }

    public class PurchaseOrderRow
    {
        public int POID { get; set; }
        public string PONumber { get; set; } = string.Empty;
        public string OrderDate { get; set; } = string.Empty;
        public string ExpectedDate { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string Carrier { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
        public string RejectionReason { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public bool IsSelected { get; set; }
    }

    public class PurchaseOrderItemRow
    {
        public int POItemID { get; set; }
        public int ProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal OrderedQty { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal? ShippedQty { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class ShippedQtyInput
    {
        public int POItemID { get; set; }
        public decimal Quantity { get; set; }
    }
}

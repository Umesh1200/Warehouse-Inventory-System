using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using IMS.Services;
using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System;

namespace IMS.Pages.Purchasing
{
    public class PurchaseOrdersModel : PageModel
    {
        private readonly IPurchaseOrderService _poService;
        private readonly IUserContext _userContext;
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public PurchaseOrdersModel(IPurchaseOrderService poService, IUserContext userContext)
        {
            _poService = poService;
            _userContext = userContext;
        }

        public List<POSummaryCard> SummaryCards { get; set; } = new();
        public List<PurchaseOrderRow> PurchaseOrders { get; set; } = new();
        public PurchaseOrderRow? SelectedPurchaseOrder { get; set; }
        public int TotalOrdersCount { get; set; }
        public List<PurchaseOrderItemRow> SelectedItems { get; set; } = new();
        public List<string> Suppliers { get; set; } = new();
        public List<string> Statuses { get; set; } = new();

        public List<dynamic> ActiveSuppliersList { get; set; } = new();
        public List<PurchaseProductOption> ActiveProductsList { get; set; } = new();
        public string SupplierProductsJson { get; set; } = "[]";
        public string SelectedItemsJson { get; set; } = "[]";

        [BindProperty]
        public PurchaseOrderCreateDto NewPurchaseOrder { get; set; } = new();
        
        [BindProperty]
        public int NewItemProductID { get; set; }
        [BindProperty]
        public decimal NewItemOrderedQty { get; set; }
        [BindProperty]
        public decimal NewItemUnitPrice { get; set; }

        [BindProperty]
        public PurchaseOrderUpdateDto EditPurchaseOrder { get; set; } = new();

        [BindProperty]
        public int EditItemProductID { get; set; }
        [BindProperty]
        public decimal EditItemOrderedQty { get; set; }
        [BindProperty]
        public decimal EditItemUnitPrice { get; set; }

        [BindProperty]
        public int ConfirmPOID { get; set; }

        [BindProperty]
        public int SubmitApprovalPOID { get; set; }

        [BindProperty]
        public int RejectPOID { get; set; }

        [BindProperty]
        public string RejectReason { get; set; } = string.Empty;

        [BindProperty]
        public int CancelPOID { get; set; }

        public async Task OnGetAsync(int? poid)
        {
            await LoadDataAsync(poid);
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (NewPurchaseOrder.Items.Count == 0 && NewItemProductID > 0 && NewItemOrderedQty > 0)
            {
                NewPurchaseOrder.Items.Add(new PurchaseOrderItemCreateDto
                {
                    ProductID = NewItemProductID,
                    OrderedQty = NewItemOrderedQty,
                    UnitPrice = NewItemUnitPrice
                });
            }

            NewPurchaseOrder.CreatedBy = _userContext.CurrentUserID;

            var result = await _poService.CreatePurchaseOrderAsync(NewPurchaseOrder);
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage(new { poid = result.NewID });
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
            if (EditPurchaseOrder.Items.Count == 0 && EditItemProductID > 0 && EditItemOrderedQty > 0)
            {
                EditPurchaseOrder.Items.Add(new PurchaseOrderItemCreateDto
                {
                    ProductID = EditItemProductID,
                    OrderedQty = EditItemOrderedQty,
                    UnitPrice = EditItemUnitPrice
                });
            }

            var result = await _poService.UpdatePurchaseOrderAsync(EditPurchaseOrder);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage(new { poid = EditPurchaseOrder.POID });
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadDataAsync(EditPurchaseOrder.POID);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostSubmitForApprovalAsync()
        {
            if (SubmitApprovalPOID <= 0)
            {
                ErrorMessage = "Invalid PO ID.";
                await LoadDataAsync(null);
                return Page();
            }

            var result = await _poService.SubmitForApprovalAsync(SubmitApprovalPOID, _userContext.CurrentUserID);
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage(new { poid = SubmitApprovalPOID });
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadDataAsync(SubmitApprovalPOID);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostConfirmAsync()
        {
            if (ConfirmPOID <= 0)
            {
                ErrorMessage = "Invalid PO ID.";
                await LoadDataAsync(null);
                return Page();
            }
            
            var result = await _poService.ConfirmPurchaseOrderAsync(ConfirmPOID, _userContext.CurrentUserID);
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage(new { poid = ConfirmPOID });
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadDataAsync(ConfirmPOID);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostRejectApprovalAsync()
        {
            if (RejectPOID <= 0)
            {
                ErrorMessage = "Invalid PO ID.";
                await LoadDataAsync(null);
                return Page();
            }

            var result = await _poService.RejectApprovalAsync(RejectPOID, _userContext.CurrentUserID, RejectReason);

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

        public async Task<IActionResult> OnPostCancelAsync()
        {
            if (CancelPOID <= 0)
            {
                ErrorMessage = "Invalid PO ID.";
                await LoadDataAsync(null);
                return Page();
            }
            
            var result = await _poService.CancelPurchaseOrderAsync(CancelPOID);
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage(new { poid = CancelPOID });
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadDataAsync(CancelPOID);
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

                // Initial page load: fetch first 50 orders
                var (pagedData, totalCount) = await _poService.GetPagedPurchaseOrdersAsync(0, 50, null, null, null);
                var poData = pagedData.ToList();
                TotalOrdersCount = totalCount;

                var summary = await _poService.GetPurchaseOrderSummaryAsync();

                // Dropdowns
                ActiveSuppliersList = (await _poService.GetActiveSuppliersAsync()).ToList();
                var products = (await _poService.GetActiveProductsAsync()).ToList();
                ActiveProductsList = products.Select(p => new PurchaseProductOption
                {
                    ID = p.ID,
                    SKU = p.SKU,
                    Name = p.Name,
                    ProductType = p.ProductType,
                    Category = p.Category,
                    GroupName = string.IsNullOrWhiteSpace(p.ProductType) ? p.Category : p.ProductType
                }).ToList();

                var mappings = (await _poService.GetSupplierProductMappingsAsync()).ToList();
                SupplierProductsJson = JsonSerializer.Serialize(mappings.Select(m => new
                {
                    supplierProductID = m.SupplierProductID,
                    supplierID = m.SupplierID,
                    productID = m.ProductID,
                    supplierSKU = m.SupplierSKU,
                    lastQuotedPrice = m.LastQuotedPrice,
                    leadTimeDays = m.LeadTimeDays,
                    minOrderQty = m.MinOrderQty,
                    packSize = m.PackSize,
                    isPreferred = m.IsPreferred,
                    status = m.Status
                }), new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                // Filter lists
                Suppliers = new List<string> { "All Suppliers" };
                Suppliers.AddRange(ActiveSuppliersList.Select(s => (string)s.Name).Distinct());

                Statuses = new List<string> { "All Statuses", "Draft", "Pending Approval", "Confirmed", "InTransit", "Received", "QCCheck", "Putaway", "Closed", "Rejected", "Cancelled" };

                // Build trend cards
                var allOrdersForTrends = (await _poService.GetPurchaseOrdersAsync()).ToList();
                var today = DateTime.Today;
                var days = Enumerable.Range(0, 6)
                    .Select(offset => today.AddDays(-5 + offset))
                    .ToList();

                SummaryCards = new List<POSummaryCard>
                {
                    BuildSummaryCard("Total Orders", summary.TotalOrders, "shopping_cart", "total", allOrdersForTrends, days, _ => true),
                    BuildSummaryCard("Drafts & Requests", summary.DraftOrders, "edit_document", "draft", allOrdersForTrends, days, o => o.Status == "Draft" || o.Status == "Pending Approval"),
                    BuildSummaryCard("Confirmed", summary.PendingConfirmedOrders, "verified", "confirmed", allOrdersForTrends, days, o => o.Status == "Confirmed"),
                    BuildSummaryCard("In Transit", summary.InTransitOrders, "local_shipping", "transit", allOrdersForTrends, days, o => o.Status == "InTransit")
                };

                // Map Rows
                PurchaseOrders = poData.Select(o => new PurchaseOrderRow
                {
                    POID = o.POID,
                    SupplierID = o.SupplierID,
                    RawOrderDate = o.OrderDate.ToString("yyyy-MM-dd"),
                    RawExpectedDate = o.ExpectedDate?.ToString("yyyy-MM-dd") ?? "",
                    Remarks = o.Remarks,
                    PONumber = o.PONumber,
                    Supplier = o.SupplierName,
                    OrderDate = o.OrderDate.ToString("MMM dd, yyyy"),
                    ExpectedDate = o.ExpectedDate?.ToString("MMM dd, yyyy") ?? "TBD",
                    TotalAmount = $"Rs. {o.TotalAmount:N2}",
                    RawTotalAmount = o.TotalAmount,
                    ItemCount = o.ItemCount,
                    TotalOrderedQty = o.TotalOrderedQty,
                    TotalReceivedQty = o.TotalReceivedQty,
                    ProgressPercent = o.TotalOrderedQty > 0 ? Math.Min(100, (int)Math.Round((o.TotalReceivedQty / o.TotalOrderedQty) * 100)) : 0,
                    Status = o.Status,
                    StatusColor = GetStatusColor(o.Status),
                    CreatedByName = o.CreatedByName,
                    ApprovedByName = o.ApprovedByName,
                    SupplierPaymentTerms = o.SupplierPaymentTerms,
                    SupplierLeadTimeDays = o.SupplierLeadTimeDays,
                    SupplierStatus = o.SupplierStatus,
                    RejectionReason = o.RejectionReason,
                    IsSelected = selectedPoid.HasValue && o.POID == selectedPoid.Value
                }).ToList();

                SelectedPurchaseOrder = PurchaseOrders.FirstOrDefault(o => o.IsSelected) ?? PurchaseOrders.FirstOrDefault();
                if (SelectedPurchaseOrder != null)
                {
                    SelectedPurchaseOrder.IsSelected = true;

                    // Fetch full details if needed
                    var details = await _poService.GetPurchaseOrderDetailsAsync(SelectedPurchaseOrder.POID);
                    if (details != null)
                    {
                        SelectedPurchaseOrder.SupplierPaymentTerms = string.IsNullOrWhiteSpace(details.SupplierPaymentTerms) ? "Net 30" : details.SupplierPaymentTerms;
                        SelectedPurchaseOrder.SupplierLeadTimeDays = details.SupplierLeadTimeDays;
                        SelectedPurchaseOrder.SupplierStatus = string.IsNullOrWhiteSpace(details.SupplierStatus) ? "Active" : details.SupplierStatus;
                        SelectedPurchaseOrder.CreatedByName = details.CreatedByName;
                        SelectedPurchaseOrder.ApprovedByName = details.ApprovedByName;
                        SelectedPurchaseOrder.RejectionReason = details.RejectionReason;
                    }

                    var items = await _poService.GetPurchaseOrderItemsAsync(SelectedPurchaseOrder.POID);
                    SelectedItems = items.Select(i => new PurchaseOrderItemRow
                    {
                        ProductID = i.ProductID,
                        RawUnitPrice = i.UnitPrice,
                        RawTotalPrice = i.TotalPrice,
                        SKU = i.SKU,
                        ProductName = i.ProductName,
                        OrderedQty = i.OrderedQty,
                        ReceivedQty = i.ReceivedQty,
                        UnitPrice = $"Rs. {i.UnitPrice:N2}",
                        TotalPrice = $"Rs. {i.TotalPrice:N2}",
                        CurrentStock = i.CurrentStock,
                        AvailableStock = i.AvailableStock,
                        ReorderLevel = i.ReorderLevel,
                        IsLowStock = i.IsLowStock,
                        ReceivedPercent = i.OrderedQty > 0 ? Math.Min(100, (int)Math.Round((i.ReceivedQty / i.OrderedQty) * 100)) : 0
                    }).ToList();

                    SelectedItemsJson = JsonSerializer.Serialize(SelectedItems.Select(i => new
                    {
                        productID = i.ProductID,
                        orderedQty = i.OrderedQty,
                        unitPrice = i.RawUnitPrice
                    }), new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                }
            }
            catch (Exception)
            {
                ErrorMessage = "Database connection failed. Showing offline mode.";
                PurchaseOrders = new List<PurchaseOrderRow>();
                SelectedItems = new List<PurchaseOrderItemRow>();
            }
        }

        public async Task<IActionResult> OnGetOrdersAsync(int offset, int limit, string? search, string? supplier, string? status)
        {
            try
            {
                var (poData, totalCount) = await _poService.GetPagedPurchaseOrdersAsync(offset, limit, search, supplier, status);

                var rows = poData.Select(o => new
                {
                    poid = o.POID,
                    supplierID = o.SupplierID,
                    remarks = o.Remarks,
                    poNumber = o.PONumber,
                    supplier = o.SupplierName,
                    orderDate = o.OrderDate.ToString("MMM dd, yyyy"),
                    expectedDate = o.ExpectedDate?.ToString("MMM dd, yyyy") ?? "TBD",
                    totalAmount = $"Rs. {o.TotalAmount:N2}",
                    itemCount = o.ItemCount,
                    totalOrderedQty = o.TotalOrderedQty,
                    totalReceivedQty = o.TotalReceivedQty,
                    progressPercent = o.TotalOrderedQty > 0 ? Math.Min(100, (int)Math.Round((o.TotalReceivedQty / o.TotalOrderedQty) * 100)) : 0,
                    status = o.Status,
                    statusColor = GetStatusColor(o.Status)
                }).ToList();

                return new JsonResult(new { success = true, orders = rows, totalCount });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        private POSummaryCard BuildSummaryCard(string title, int value, string icon, string colorClass, List<PurchaseOrderDto> orders, List<DateTime> days, Func<PurchaseOrderDto, bool> predicate)
        {
            var trend = days
                .Select(day => orders.Count(o => predicate(o) && o.OrderDate.Date == day.Date))
                .ToList();

            var previous = trend.Take(3).Sum();
            var current = trend.Skip(3).Sum();
            var trendLabel = current >= previous ? $"+{current - previous} vs early week" : $"{current - previous} vs early week";

            return new POSummaryCard
            {
                Title = title,
                Value = value.ToString(),
                Icon = icon,
                ColorClass = colorClass,
                TrendPoints = string.Join(",", trend),
                TrendLabel = trendLabel
            };
        }

        private string GetStatusColor(string status)
        {
            return status switch
            {
                "Closed" => "success",
                "Putaway" => "success",
                "Received" => "success",
                "InTransit" => "info",
                "Confirmed" => "primary",
                "Pending Approval" => "warning",
                "Draft" => "secondary",
                "Rejected" => "danger",
                "Cancelled" => "danger",
                _ => "warning"
            };
        }
    }

    public class POSummaryCard
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
        public string TrendPoints { get; set; } = string.Empty;
        public string TrendLabel { get; set; } = string.Empty;
    }

    public class PurchaseOrderRow
    {
        public int POID { get; set; }
        public int SupplierID { get; set; }
        public string RawOrderDate { get; set; } = string.Empty;
        public string RawExpectedDate { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;

        public string PONumber { get; set; } = string.Empty;
        public string Supplier { get; set; } = string.Empty;
        public string OrderDate { get; set; } = string.Empty;
        public string ExpectedDate { get; set; } = string.Empty;
        public string TotalAmount { get; set; } = string.Empty;
        public decimal RawTotalAmount { get; set; }
        public int ItemCount { get; set; }
        public decimal TotalOrderedQty { get; set; }
        public decimal TotalReceivedQty { get; set; }
        public int ProgressPercent { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public string CreatedByName { get; set; } = string.Empty;
        public string ApprovedByName { get; set; } = string.Empty;
        public string SupplierPaymentTerms { get; set; } = "Net 30";
        public int SupplierLeadTimeDays { get; set; }
        public string SupplierStatus { get; set; } = "Active";
        public string RejectionReason { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    public class PurchaseOrderItemRow
    {
        public int ProductID { get; set; }
        public decimal RawUnitPrice { get; set; }
        public decimal RawTotalPrice { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal OrderedQty { get; set; }
        public decimal ReceivedQty { get; set; }
        public string UnitPrice { get; set; } = string.Empty;
        public string TotalPrice { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
        public decimal AvailableStock { get; set; }
        public decimal ReorderLevel { get; set; }
        public bool IsLowStock { get; set; }
        public int ReceivedPercent { get; set; }
    }

    public class PurchaseProductOption
    {
        public int ID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
    }
}

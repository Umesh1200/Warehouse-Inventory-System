using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using IMS.Services;
using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace IMS.Pages.Purchasing
{
    public class SuppliersModel : PageModel
    {
        private readonly ISupplierService _supplierService;
        private readonly IPurchaseOrderService _purchaseOrderService;
        public string ErrorMessage { get; set; } = string.Empty;

        public SuppliersModel(ISupplierService supplierService, IPurchaseOrderService purchaseOrderService)
        {
            _supplierService = supplierService;
            _purchaseOrderService = purchaseOrderService;
        }

        public List<SupplierSummaryCard> SummaryCards { get; set; } = new();
        public List<SupplierRow> Suppliers { get; set; } = new();
        public List<string> SupplierTypes { get; set; } = new();
        public List<PurchaseProductOption> ProductsList { get; set; } = new();
        public List<SupplierProductViewRow> SupplierProducts { get; set; } = new();

        [BindProperty]
        public SupplierCreateDto NewSupplier { get; set; } = new();

        [BindProperty]
        public SupplierUpdateDto EditSupplier { get; set; } = new();

        [BindProperty]
        public int DeactivateSupplierID { get; set; }

        [BindProperty]
        public SupplierProductDto SupplierProduct { get; set; } = new();

        [BindProperty]
        public int BlockSupplierProductID { get; set; }

        public string SuccessMessage { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return Page();
            }

            var result = await _supplierService.CreateSupplierAsync(NewSupplier);
            
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

            var result = await _supplierService.UpdateSupplierAsync(EditSupplier);

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

        public async Task<IActionResult> OnPostDeactivateAsync()
        {
            if (DeactivateSupplierID <= 0)
            {
                ErrorMessage = "Invalid Supplier ID for deactivation.";
                await LoadDataAsync();
                return Page();
            }

            var result = await _supplierService.DeactivateSupplierAsync(DeactivateSupplierID);

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

        public async Task<IActionResult> OnPostSaveSupplierProductAsync()
        {
            var result = await _purchaseOrderService.SaveSupplierProductMappingAsync(SupplierProduct);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage();
            }

            ErrorMessage = result.Message;
            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostBlockSupplierProductAsync()
        {
            var result = await _purchaseOrderService.BlockSupplierProductMappingAsync(BlockSupplierProductID);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage();
            }

            ErrorMessage = result.Message;
            await LoadDataAsync();
            return Page();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                if (TempData["SuccessMessage"] != null)
                {
                    SuccessMessage = TempData["SuccessMessage"]?.ToString() ?? "";
                }

                SupplierTypes = new List<string> { "All Types", "Manufacturer", "Distributor", "Service Provider", "Importer" };

                var suppliersData = (await _supplierService.GetSuppliersAsync()).ToList();
                ProductsList = (await _purchaseOrderService.GetActiveProductsAsync())
                    .Select(p => new PurchaseProductOption
                    {
                        ID = p.ID,
                        SKU = p.SKU,
                        Name = p.Name,
                        ProductType = p.ProductType,
                        Category = p.Category,
                        GroupName = !string.IsNullOrWhiteSpace(p.ProductType) ? p.ProductType : p.Category
                    })
                    .OrderBy(p => p.GroupName)
                    .ThenBy(p => p.Name)
                    .ToList();
                var mappings = (await _purchaseOrderService.GetSupplierProductMappingsAsync()).ToList();
                var summaryData = await _supplierService.GetSupplierSummaryAsync();
                var today = DateTime.Today;
                var lastSevenDays = Enumerable.Range(6, 7)
                    .Select(offset => today.AddDays(-offset))
                    .ToList();

                SummaryCards = new List<SupplierSummaryCard>
                {
                    BuildSummaryCard("Total Suppliers", summaryData.TotalSuppliers, "group", "primary", suppliersData, lastSevenDays, _ => true),
                    BuildSummaryCard("Active Suppliers", summaryData.ActiveSuppliers, "check_circle", "success", suppliersData, lastSevenDays, s => s.Status == "Active"),
                    BuildSummaryCard("Pending Review", summaryData.PendingReview, "rate_review", "warning", suppliersData, lastSevenDays, s => s.Status == "Pending Review"),
                    BuildSummaryCard("Avg. Lead Time", summaryData.AverageLeadTime, "schedule", "info", suppliersData, lastSevenDays, _ => true, " Days")
                };

                // Map Supplier Rows
                Suppliers = suppliersData.Select(s => new SupplierRow
                {
                    SupplierID = s.SupplierID,
                    Code = s.SupplierCode,
                    Name = s.SupplierName,
                    ContactPerson = s.ContactPerson,
                    Phone = s.Phone,
                    Email = s.Email,
                    Location = s.Address, // Reuse address for location if needed or just address
                    Type = s.SupplierType,
                    LeadTimeDays = s.LeadTimeDays,
                    PaymentTerms = s.PaymentTerms,
                    Status = s.Status,
                    ReturnPolicy = s.ReturnPolicy,
                    ReturnEmail = s.ReturnEmail,
                    PreferredCarrier = s.PreferredCarrier
                }).ToList();

                SupplierProducts = mappings.Select(m =>
                {
                    var supplier = suppliersData.FirstOrDefault(s => s.SupplierID == m.SupplierID);
                    var product = ProductsList.FirstOrDefault(p => p.ID == m.ProductID);
                    return new SupplierProductViewRow
                    {
                        SupplierProductID = m.SupplierProductID,
                        SupplierID = m.SupplierID,
                        SupplierName = supplier?.SupplierName ?? "",
                        ProductID = m.ProductID,
                        SKU = product?.SKU ?? "",
                        ProductName = product?.Name ?? "",
                        ProductType = product?.ProductType ?? "",
                        SupplierSKU = m.SupplierSKU,
                        LastQuotedPrice = m.LastQuotedPrice,
                        LeadTimeDays = m.LeadTimeDays,
                        MinOrderQty = m.MinOrderQty,
                        PackSize = m.PackSize,
                        IsPreferred = m.IsPreferred,
                        Status = m.Status
                    };
                }).ToList();
            }
            catch (Exception)
            {
                ErrorMessage = "Database connection failed. Showing offline mode.";
                Suppliers = new List<SupplierRow>();
                SupplierProducts = new List<SupplierProductViewRow>();
            }
        }

        private SupplierSummaryCard BuildSummaryCard(string title, int value, string icon, string colorClass, List<SupplierDto> suppliers, List<DateTime> days, Func<SupplierDto, bool> predicate, string suffix = "")
        {
            var dailyValues = days
                .Select(day => suppliers.Count(supplier => supplier.CreatedAt.Date == day.Date && predicate(supplier)))
                .ToList();

            var max = Math.Max(1, dailyValues.Max());
            var points = dailyValues.Select((count, index) =>
            {
                var x = index * 16;
                var y = 36 - ((decimal)count / max * 30);
                return $"{x:0.##},{y:0.##}";
            });

            return new SupplierSummaryCard
            {
                Title = title,
                Value = $"{value:N0}{suffix}",
                Icon = icon,
                ColorClass = colorClass,
                TrendPoints = string.Join(" ", points),
                TrendLabel = $"{dailyValues.Sum():N0} new in last 7 days"
            };
        }
    }

    public class SupplierSummaryCard
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
        public string TrendPoints { get; set; } = string.Empty;
        public string TrendLabel { get; set; } = string.Empty;
    }

    public class SupplierRow
    {
        public int SupplierID { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int LeadTimeDays { get; set; }
        public string PaymentTerms { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ReturnPolicy { get; set; } = string.Empty;
        public string ReturnEmail { get; set; } = string.Empty;
        public string PreferredCarrier { get; set; } = string.Empty;
    }

    public class SupplierProductViewRow
    {
        public int SupplierProductID { get; set; }
        public int SupplierID { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public int ProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string SupplierSKU { get; set; } = string.Empty;
        public decimal? LastQuotedPrice { get; set; }
        public int LeadTimeDays { get; set; }
        public decimal MinOrderQty { get; set; }
        public decimal PackSize { get; set; }
        public bool IsPreferred { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}

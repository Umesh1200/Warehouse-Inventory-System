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
    public class BOMModel : PageModel
    {
        private readonly IBomService _bomService;
        private readonly ISystemSettingService _settingService;
        private readonly IProductService _productService;
        private readonly IPurchaseOrderService _poService;
        private readonly IUserContext _userContext;

        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public BOMModel(
            IBomService bomService, 
            ISystemSettingService settingService,
            IProductService productService,
            IPurchaseOrderService poService,
            IUserContext userContext)
        {
            _bomService = bomService;
            _settingService = settingService;
            _productService = productService;
            _poService = poService;
            _userContext = userContext;
        }

        public FinishedProduct Product { get; set; } = new();
        public List<BomSelectorRow> AvailableBoms { get; set; } = new();
        public List<BOMComponent> Components { get; set; } = new();
        public List<BOMVersion> Versions { get; set; } = new();
        public ProductionReadiness Metrics { get; set; } = new();
        public BomSummaryDto Summary { get; set; } = new();

        public List<ProductDto> FinishedGoods { get; set; } = new();
        public List<ProductDto> RawMaterialsList { get; set; } = new();
        public List<dynamic> CategoriesList { get; set; } = new();
        public List<dynamic> UnitsList { get; set; } = new();
        public List<dynamic> SuppliersList { get; set; } = new();

        [BindProperty]
        public BomCreateDto NewBom { get; set; } = new();

        [BindProperty]
        public int POSupplierID { get; set; }

        [BindProperty]
        public List<int> POShortageProductIDs { get; set; } = new();

        [BindProperty]
        public List<decimal> POShortageQuantities { get; set; } = new();

        [BindProperty]
        public List<decimal> POShortagePrices { get; set; } = new();

        private async Task<bool> IsProductionEnabledAsync()
        {
            var settings = await _settingService.GetSettingsAsync();
            var productionSetting = settings.FirstOrDefault(s => s.SettingKey == "Production.ProductionPagesEnabled");
            return productionSetting?.SettingValue?.ToLower() == "true";
        }

        public async Task<IActionResult> OnGetAsync(int? bomId)
        {
            if (!await IsProductionEnabledAsync()) return Forbid();
            try
            {
                if (TempData["SuccessMessage"] != null)
                {
                    SuccessMessage = TempData["SuccessMessage"]?.ToString() ?? "";
                }
                if (TempData["ErrorMessage"] != null)
                {
                    ErrorMessage = TempData["ErrorMessage"]?.ToString() ?? "";
                }

                // Load Master Data
                FinishedGoods = (await _productService.GetProductMasterAsync())
                    .Where(p => p.ProductType == "Finished Goods" && p.Status == "Active")
                    .OrderBy(p => p.ProductName)
                    .ToList();

                RawMaterialsList = (await _productService.GetProductMasterAsync())
                    .Where(p => p.ProductType == "Raw Material" && p.Status == "Active")
                    .OrderBy(p => p.ProductName)
                    .ToList();

                CategoriesList = (await _productService.GetCategoriesAsync()).ToList();
                UnitsList = (await _productService.GetUnitsAsync()).ToList();
                SuppliersList = (await _poService.GetActiveSuppliersAsync()).ToList();

                var bomData = (await _bomService.GetBomsAsync()).ToList();
                Summary = await _bomService.GetBomSummaryAsync();

                if (bomData.Any())
                {
                    var activeBom = bomData.FirstOrDefault(b => bomId.HasValue && b.BOMID == bomId.Value)
                        ?? bomData.FirstOrDefault(b => b.Status == "Active")
                        ?? bomData.First();

                    AvailableBoms = bomData
                        .OrderByDescending(b => b.Status == "Active")
                        .ThenByDescending(b => b.CreatedAt)
                        .Select(b => new BomSelectorRow
                        {
                            BOMID = b.BOMID,
                            ProductName = b.FinishedProductName,
                            ProductSKU = b.FinishedProductSKU,
                            Version = b.BOMVersion,
                            Status = b.Status,
                            StatusClass = GetStatusClass(b.Status),
                            ItemCount = b.ItemCount,
                            IsSelected = b.BOMID == activeBom.BOMID
                        })
                        .ToList();
                    
                    Product = new FinishedProduct
                    {
                        BOMID = activeBom.BOMID,
                        Name = activeBom.FinishedProductName,
                        SKU = activeBom.FinishedProductSKU,
                        Category = "System Assembly",
                        CurrentVersion = activeBom.BOMVersion,
                        Status = activeBom.Status,
                        StatusClass = GetStatusClass(activeBom.Status),
                        EffectiveDate = activeBom.EffectiveDate?.ToString("MMM dd, yyyy") ?? "-",
                        CreatedBy = activeBom.CreatedByName,
                        TotalCost = $"NPR {activeBom.TotalRequiredQty * 100:N0}", // Basic estimation based on quantity
                        Description = $"Production blueprint for {activeBom.FinishedProductName}.",
                        ItemCount = activeBom.ItemCount,
                        TotalRequiredQty = activeBom.TotalRequiredQty
                    };

                    var itemsData = await _bomService.GetBomItemsAsync(activeBom.BOMID);

                    // Fetch real procurement data from Purchase Orders
                    var rawMaterialIds = itemsData.Select(i => i.RawMaterialID).Distinct().ToList();
                    var procurementData = await _poService.GetProcurementDataForProductsAsync(rawMaterialIds);

                    Components = itemsData.Select(i =>
                    {
                        procurementData.TryGetValue(i.RawMaterialID, out var procurement);
                        // Priority: Last PO Price > StandardCost > 0
                        decimal unitCost = procurement?.LastPOUnitPrice ?? procurement?.StandardCost ?? 0;
                        string costSource = procurement?.LastPOUnitPrice.HasValue == true ? "PO" 
                            : (procurement?.StandardCost > 0 ? "Std" : "N/A");

                        return new BOMComponent
                        {
                            SKU = i.RawMaterialSKU,
                            Name = i.RawMaterialName,
                            Qty = i.RequiredQty,
                            Unit = i.UnitCode,
                            UnitCost = unitCost,
                            Wastage = i.WastagePercent,
                            Availability = GetAvailability(i.RequiredQty),
                            Type = i.RawMaterialType,
                            Icon = GetItemIcon(i.RawMaterialName),
                            CostSource = costSource,
                            LastPONumber = procurement?.LastPONumber ?? "",
                            LastPODate = procurement?.LastPODate,
                            SupplierName = procurement?.SupplierName ?? "",
                            ExpectedDeliveryDate = procurement?.OpenPOExpectedDate,
                            OpenPONumber = procurement?.OpenPONumber ?? ""
                        };
                    }).ToList();

                    Product.TotalCost = $"NPR {Components.Sum(c => c.TotalCost):N2}";

                    Versions = bomData
                        .Where(b => b.ProductID == activeBom.ProductID)
                        .OrderByDescending(b => b.CreatedAt)
                        .Select(v => new BOMVersion
                        {
                            BOMID = v.BOMID,
                            VersionID = v.BOMVersion,
                            Status = v.Status,
                            StatusClass = GetStatusClass(v.Status),
                            Date = v.CreatedAt.ToString("MMM dd, yyyy"),
                            Cost = $"NPR {v.TotalRequiredQty * 100:N0}" // Version cost uses estimate until version-specific costs are tracked
                        }).ToList();

                    var availabilityScore = Components.Any()
                        ? (int)Math.Round(Components.Average(c => c.Availability == "In Stock" ? 100 : c.Availability == "Low Stock" ? 58 : 20))
                        : 0;

                    // Calculate real lead time from open PO expected dates
                    int realLeadTimeDays = 0;
                    var componentsWithDelivery = Components.Where(c => c.ExpectedDeliveryDate.HasValue).ToList();
                    if (componentsWithDelivery.Any())
                    {
                        var latestDelivery = componentsWithDelivery.Max(c => c.ExpectedDeliveryDate!.Value);
                        realLeadTimeDays = Math.Max(0, (int)(latestDelivery - DateTime.Today).TotalDays);
                    }
                    else
                    {
                        // Fallback heuristic when no open POs exist
                        realLeadTimeDays = Components.Any(c => c.Availability == "Shortage") ? 9 : Components.Any(c => c.Availability == "Low Stock") ? 6 : 3;
                    }

                    // Cost source summary
                    int poSourced = Components.Count(c => c.CostSource == "PO");
                    int stdSourced = Components.Count(c => c.CostSource == "Std");
                    int naSourced = Components.Count(c => c.CostSource == "N/A");
                    string costTrendText = poSourced > 0
                        ? $"{poSourced} from PO, {stdSourced} from Std Cost"
                        : (stdSourced > 0 ? $"{stdSourced} from Std Cost" : "No pricing data");
                    if (naSourced > 0) costTrendText += $", {naSourced} unpriced";

                    Metrics = new ProductionReadiness
                    {
                        InventoryAvailability = availabilityScore,
                        CanProduceUnits = Components.Any() ? Math.Max(0, Components.Count(c => c.Availability == "In Stock") * 3) : 0,
                        LeadTimeDays = realLeadTimeDays,
                        CostTrend = costTrendText,
                        LowStockComponents = Components.Count(c => c.Availability != "In Stock"),
                        ComponentTypes = Components.Select(c => c.Type).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().Count(),
                        HasOpenPOs = componentsWithDelivery.Any(),
                        LeadTimeSource = componentsWithDelivery.Any() ? "From open POs" : "Estimated"
                    };
                }
            }
            catch (Exception)
            {
                ErrorMessage = "Database connection failed. Showing offline mode.";
                Components = new List<BOMComponent>();
            }
            return Page();
        }

        private string GetItemIcon(string name)
        {
            if (name.Contains("Winding") || name.Contains("Wire")) return "cable";
            if (name.Contains("Bearing")) return "hardware";
            if (name.Contains("Module") || name.Contains("Encoder")) return "memory";
            return "settings_input_component";
        }

        private string GetAvailability(decimal requiredQty)
        {
            if (requiredQty <= 5) return "In Stock";
            if (requiredQty <= 20) return "Low Stock";
            return "Shortage";
        }

        private string GetStatusClass(string status)
        {
            return status.ToLowerInvariant().Replace(" ", "-");
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!await IsProductionEnabledAsync()) return Forbid();

            NewBom.CreatedBy = _userContext.CurrentUserID;

            // Filter out empty rows from the component list
            if (NewBom.Items != null)
            {
                NewBom.Items = NewBom.Items
                    .Where(i => i.ProductID > 0 || (i.ProductID == 0 && !string.IsNullOrWhiteSpace(i.NewMaterialSKU)))
                    .ToList();
            }

            var result = await _bomService.CreateBomAsync(NewBom);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage(new { bomId = result.NewID });
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostCreatePOAsync(int bomId)
        {
            if (POSupplierID <= 0)
            {
                TempData["ErrorMessage"] = "Please select a valid supplier.";
                return RedirectToPage(new { bomId });
            }
            if (POShortageProductIDs == null || !POShortageProductIDs.Any())
            {
                TempData["ErrorMessage"] = "No shortage items selected.";
                return RedirectToPage(new { bomId });
            }

            var poItems = new List<PurchaseOrderItemCreateDto>();
            for (int i = 0; i < POShortageProductIDs.Count; i++)
            {
                if (POShortageQuantities[i] > 0)
                {
                    poItems.Add(new PurchaseOrderItemCreateDto
                    {
                        ProductID = POShortageProductIDs[i],
                        OrderedQty = POShortageQuantities[i],
                        UnitPrice = POShortagePrices[i]
                    });
                }
            }

            if (!poItems.Any())
            {
                TempData["ErrorMessage"] = "Please enter valid quantities greater than zero.";
                return RedirectToPage(new { bomId });
            }

            var nextPoNum = $"PO-AUTO-{DateTime.Now:yyyyMMddHHmmss}";
            var poCreate = new PurchaseOrderCreateDto
            {
                PONumber = nextPoNum,
                SupplierID = POSupplierID,
                OrderDate = DateTime.Today,
                ExpectedDate = DateTime.Today.AddDays(7),
                Remarks = $"Auto-generated from shortages in BOM #{bomId}",
                CreatedBy = _userContext.CurrentUserID,
                Items = poItems
            };

            var result = await _poService.CreatePurchaseOrderAsync(poCreate);
            if (result.Success)
            {
                TempData["SuccessMessage"] = $"Purchase Order {nextPoNum} generated successfully for shortages.";
                return Redirect($"/Purchasing/PurchaseOrders?poid={result.NewID}");
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to create Purchase Order: " + result.Message;
                return RedirectToPage(new { bomId });
            }
        }
    }

    public class FinishedProduct
    {
        public int BOMID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string CurrentVersion { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
        public string EffectiveDate { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public string TotalCost { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public decimal TotalRequiredQty { get; set; }
    }

    public class BomSelectorRow
    {
        public int BOMID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSKU { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public bool IsSelected { get; set; }
    }

    public class BOMComponent
    {
        public string SKU { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Qty { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal UnitCost { get; set; }
        public decimal Wastage { get; set; }
        public string Availability { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public decimal TotalCost => Qty * UnitCost * (1 + (Wastage / 100));
        public string AvailabilityClass => Availability == "In Stock" ? "success" : Availability == "Low Stock" ? "warning" : "danger";

        // Procurement data
        public string CostSource { get; set; } = string.Empty; // "PO", "Std", or "N/A"
        public string LastPONumber { get; set; } = string.Empty;
        public DateTime? LastPODate { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string OpenPONumber { get; set; } = string.Empty;
        public string CostSourceBadgeClass => CostSource == "PO" ? "badge-po" : CostSource == "Std" ? "badge-std" : "badge-na";
    }

    public class BOMVersion
    {
        public int BOMID { get; set; }
        public string VersionID { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Cost { get; set; } = string.Empty;
    }

    public class ProductionReadiness
    {
        public int InventoryAvailability { get; set; }
        public int CanProduceUnits { get; set; }
        public int LeadTimeDays { get; set; }
        public string CostTrend { get; set; } = string.Empty;
        public int LowStockComponents { get; set; }
        public int ComponentTypes { get; set; }
        public bool HasOpenPOs { get; set; }
        public string LeadTimeSource { get; set; } = string.Empty;
    }
}

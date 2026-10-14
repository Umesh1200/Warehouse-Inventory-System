using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using IMS.Services;
using IMS.Models.DTOs;
using System.Text.Json;

namespace IMS.Pages.Inventory
{
    public class OverviewModel : PageModel
    {
        private readonly IInventoryService _inventoryService;
        private readonly IProductService _productService;
        public string ErrorMessage { get; set; } = string.Empty;

        public OverviewModel(IInventoryService inventoryService, IProductService productService)
        {
            _inventoryService = inventoryService;
            _productService = productService;
        }

        [BindProperty(SupportsGet = true)] public string? Warehouse { get; set; }
        [BindProperty(SupportsGet = true)] public string? Category { get; set; }
        [BindProperty(SupportsGet = true)] public string? Status { get; set; }
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public List<InventoryItem> InventoryItems { get; set; } = new();
        public List<string> Warehouses { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public List<string> Statuses { get; set; } = new();
        public InventorySummaryDto Summary { get; set; } = new();
        public string StockHealthChartJson { get; set; } = "{}";
        public string QuantityMixChartJson { get; set; } = "{}";
        public string CategoryValueChartJson { get; set; } = "{}";
        public string ReorderRiskChartJson { get; set; } = "{}";

        public int FilteredTotalCount { get; set; }
        public int TotalPages { get; set; }
        public int StartItem { get; set; }
        public int EndItem { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                if (PageNumber < 1) PageNumber = 1;

                Statuses = new List<string> { "All Statuses", "In Stock", "Low Stock", "Out of Stock", "QC Hold", "Damaged" };

                var (allInventoryData, _) = await _inventoryService.GetOverviewAsync(null, null, null, 1, int.MaxValue);
                var allInventory = allInventoryData.ToList();
                Warehouses = new List<string> { "All Warehouses" };
                Warehouses.AddRange(allInventory.Select(i => i.WarehouseName).Where(w => !string.IsNullOrWhiteSpace(w)).Distinct().OrderBy(w => w));
                
                var masterCategories = (await _productService.GetCategoriesAsync()).Select(c => (string)c.Name);
                Categories = new List<string> { "All Categories" };
                var allCatNames = allInventory.Select(i => i.CategoryName)
                    .Concat(masterCategories)
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Distinct()
                    .OrderBy(c => c);
                Categories.AddRange(allCatNames);

                var (overviewData, totalCount) = await _inventoryService.GetOverviewAsync(Warehouse, Category, Status, PageNumber, PageSize);
                var (filteredAllData, _) = await _inventoryService.GetOverviewAsync(Warehouse, Category, Status, 1, int.MaxValue);
                var filteredAll = filteredAllData.ToList();

                Summary = new InventorySummaryDto
                {
                    TotalStockValue = filteredAll.Sum(x => x.TotalValue),
                    LowStockCount = filteredAll.Count(x => x.StockStatus == "Low Stock" || x.StockStatus == "Out of Stock"),
                    TotalQCHold = filteredAll.Sum(x => x.QCHoldQty),
                    TotalDamaged = filteredAll.Sum(x => x.DamagedQty)
                };
                BuildChartJson(filteredAll);

                FilteredTotalCount = totalCount;
                TotalPages = (int)Math.Ceiling(FilteredTotalCount / (double)PageSize);

                if (FilteredTotalCount == 0)
                {
                    StartItem = 0;
                    EndItem = 0;
                    PageNumber = 1;
                }
                else
                {
                    if (PageNumber > TotalPages) PageNumber = TotalPages;
                    StartItem = ((PageNumber - 1) * PageSize) + 1;
                    EndItem = Math.Min(PageNumber * PageSize, FilteredTotalCount);
                }

                // Map to UI model
                InventoryItems = overviewData.Select(d => new InventoryItem
                {
                    ProductID = d.ProductID,
                    SKU = d.SKU,
                    ProductName = d.ProductName,
                    Category = d.CategoryName,
                    CurrentQty = (int)d.CurrentQty,
                    ReservedQty = (int)d.ReservedQty,
                    QCHoldQty = (int)d.QCHoldQty,
                    DamagedQty = (int)d.DamagedQty,
                    AvailableQty = (int)d.AvailableQty,
                    ReorderPoint = (int)d.ReorderLevel,
                    Status = d.StockStatus
                }).ToList();
            }
            catch (Exception)
            {
                ErrorMessage = "Database connection failed. Showing offline mode.";
                InventoryItems = new List<InventoryItem>();
            }
        }

        private void BuildChartJson(List<InventoryOverviewDto> data)
        {
            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            var healthOrder = new[] { "In Stock", "Low Stock", "Out of Stock", "QC Hold", "Damaged" };
            StockHealthChartJson = JsonSerializer.Serialize(new
            {
                labels = healthOrder,
                values = healthOrder.Select(status => data.Count(i => i.StockStatus == status))
            }, jsonOptions);

            QuantityMixChartJson = JsonSerializer.Serialize(new
            {
                labels = new[] { "Available", "Reserved", "QC Hold", "Damaged" },
                values = new[]
                {
                    data.Sum(i => i.AvailableQty),
                    data.Sum(i => i.ReservedQty),
                    data.Sum(i => i.QCHoldQty),
                    data.Sum(i => i.DamagedQty)
                }
            }, jsonOptions);

            var categoryValue = data
                .GroupBy(i => string.IsNullOrWhiteSpace(i.CategoryName) ? "Uncategorized" : i.CategoryName)
                .Select(g => new { Category = g.Key, Value = g.Sum(i => i.TotalValue) })
                .OrderByDescending(g => g.Value)
                .Take(6)
                .ToList();

            CategoryValueChartJson = JsonSerializer.Serialize(new
            {
                labels = categoryValue.Select(c => c.Category),
                values = categoryValue.Select(c => c.Value)
            }, jsonOptions);

            var reorderRisk = data
                .Select(i => new
                {
                    i.SKU,
                    i.ProductName,
                    Shortage = Math.Max(0, i.ReorderLevel - i.AvailableQty),
                    i.AvailableQty,
                    i.ReorderLevel
                })
                .Where(i => i.Shortage > 0)
                .OrderByDescending(i => i.Shortage)
                .ThenBy(i => i.SKU)
                .Take(8)
                .ToList();

            ReorderRiskChartJson = JsonSerializer.Serialize(new
            {
                labels = reorderRisk.Select(i => i.SKU),
                values = reorderRisk.Select(i => i.Shortage),
                products = reorderRisk.Select(i => i.ProductName),
                available = reorderRisk.Select(i => i.AvailableQty),
                reorder = reorderRisk.Select(i => i.ReorderLevel)
            }, jsonOptions);
        }

        public async Task<IActionResult> OnGetExportCsvAsync()
        {
            try
            {
                // Export all filtered data (no pagination)
                var (overviewData, _) = await _inventoryService.GetOverviewAsync(Warehouse, Category, Status, 1, int.MaxValue);
                var csv = new System.Text.StringBuilder();
                csv.AppendLine("SKU,Product Name,Category,Current Qty,Reserved,QC Hold,Damaged,Available,Status");

                foreach (var item in overviewData)
                {
                    csv.AppendLine($"{EscapeCsv(item.SKU)},{EscapeCsv(item.ProductName)},{EscapeCsv(item.CategoryName)},{item.CurrentQty},{item.ReservedQty},{item.QCHoldQty},{item.DamagedQty},{item.AvailableQty},{item.StockStatus}");
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
                return File(bytes, "text/csv", $"Inventory_Overview_{DateTime.Now:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error exporting CSV: " + ex.Message;
                return RedirectToPage();
            }
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }
    }

    public class InventoryItem
    {
        public int ProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int CurrentQty { get; set; }
        public int ReservedQty { get; set; }
        public int QCHoldQty { get; set; }
        public int DamagedQty { get; set; }
        public int AvailableQty { get; set; }
        public int ReorderPoint { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}

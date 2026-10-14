using IMS.Models.DTOs;
using IMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace IMS.Pages.Warehouses
{
    public class ProductLocatorModel : PageModel
    {
        private readonly IWarehouseService _warehouseService;

        public ProductLocatorModel(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }

        [BindProperty(SupportsGet = true)]
        public string? Sku { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ProductId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchQuery { get; set; }

        public ProductLocatorDetailDto? ProductDetail { get; set; }
        public List<ProductLocatorSearchResultDto> QuickPickProducts { get; set; } = new();
        public string WarehouseLayoutJson { get; set; } = "[]";
        public string OccupiedShelfIdsJson { get; set; } = "[]";
        public string LocationsJson { get; set; } = "[]";
        public string ErrorMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Load Quick Pick / Suggested Products for instant one-click lookup
                var suggestions = await _warehouseService.SearchProductsForLocatorAsync(string.Empty);
                QuickPickProducts = suggestions.Take(8).ToList();

                // If no SKU or ProductId is provided, but SearchQuery exists
                if (string.IsNullOrWhiteSpace(Sku) && !ProductId.HasValue && !string.IsNullOrWhiteSpace(SearchQuery))
                {
                    var searchMatches = await _warehouseService.SearchProductsForLocatorAsync(SearchQuery);
                    var firstMatch = searchMatches.FirstOrDefault();
                    if (firstMatch != null)
                    {
                        ProductId = firstMatch.ProductID;
                        Sku = firstMatch.SKU;
                    }
                }

                // If SKU is specified in query, prioritize SKU
                if (!string.IsNullOrWhiteSpace(Sku))
                {
                    ProductId = null;
                }

                // If still empty and suggestions exist, pick the first one as default demo/initial view
                if (string.IsNullOrWhiteSpace(Sku) && !ProductId.HasValue && QuickPickProducts.Any())
                {
                    ProductId = QuickPickProducts.First().ProductID;
                    Sku = QuickPickProducts.First().SKU;
                }

                if (ProductId.HasValue || !string.IsNullOrWhiteSpace(Sku))
                {
                    ProductDetail = await _warehouseService.GetProductLocationDetailsAsync(ProductId, Sku);
                    if (ProductDetail == null)
                    {
                        ErrorMessage = $"Product with SKU '{Sku}' or ID '{ProductId}' was not found in the warehouse system.";
                    }
                }

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };

                // Load Warehouse Layout for 2D Grid
                var vizData = await _warehouseService.GetWarehouseVisualizationAsync();
                var distinctShelves = vizData
                    .GroupBy(s => s.ShelfID)
                    .Select(g => g.First())
                    .Select(s => new
                    {
                        warehouseId = s.WarehouseID,
                        warehouseCode = s.WarehouseCode,
                        warehouseName = s.WarehouseName,
                        floorId = s.FloorID,
                        floorCode = s.FloorCode,
                        floorName = s.FloorName,
                        roomId = s.RoomID,
                        roomCode = s.RoomCode,
                        roomName = s.RoomName,
                        rackId = s.RackID,
                        rackCode = s.RackCode,
                        rackName = s.RackName,
                        shelfId = s.ShelfID,
                        shelfCode = s.ShelfCode,
                        shelfName = s.ShelfName,
                        shelfType = s.ShelfType,
                        maxQty = s.MaxQty,
                        currentQty = s.CurrentQty,
                        utilization = Math.Round(s.UtilizationPercentage, 1),
                        isActive = s.IsActive
                    })
                    .ToList();

                WarehouseLayoutJson = JsonSerializer.Serialize(distinctShelves, jsonOptions);

                if (ProductDetail != null && ProductDetail.Locations.Any())
                {
                    var occupiedIds = ProductDetail.Locations.Select(l => l.ShelfID).Distinct().ToList();
                    OccupiedShelfIdsJson = JsonSerializer.Serialize(occupiedIds, jsonOptions);
                    LocationsJson = JsonSerializer.Serialize(ProductDetail.Locations, jsonOptions);
                }
                else
                {
                    OccupiedShelfIdsJson = "[]";
                    LocationsJson = "[]";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"An error occurred while loading product location: {ex.Message}";
            }

            return Page();
        }

        // Live AJAX autocomplete endpoint for search input
        public async Task<IActionResult> OnGetSearchAsync(string term)
        {
            var results = await _warehouseService.SearchProductsForLocatorAsync(term ?? string.Empty);
            return new JsonResult(results);
        }

        // Live AJAX details endpoint
        public async Task<IActionResult> OnGetProductLocationAsync(int? productId, string? sku)
        {
            var detail = await _warehouseService.GetProductLocationDetailsAsync(productId, sku);
            if (detail == null)
            {
                return NotFound(new { message = "Product not found" });
            }
            return new JsonResult(detail);
        }
    }
}

using Microsoft.AspNetCore.Mvc.RazorPages;
using IMS.Services;
using IMS.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace IMS.Pages.Inventory
{
    public class ProductsModel : PageModel
    {
        private readonly IProductService _productService;
        private readonly IPurchaseOrderService _purchaseOrderService;
        private readonly IProductStorageRuleService _storageRuleService;
        private readonly IWarehouseService _warehouseService;
        private readonly IUserContext _userContext;
        private readonly IWebHostEnvironment _env;
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public ProductsModel(IProductService productService, IUserContext userContext, IPurchaseOrderService purchaseOrderService, IProductStorageRuleService storageRuleService, IWarehouseService warehouseService, IWebHostEnvironment env)
        {
            _productService = productService;
            _userContext = userContext;
            _purchaseOrderService = purchaseOrderService;
            _storageRuleService = storageRuleService;
            _warehouseService = warehouseService;
            _env = env;
        }

        public List<ProductSummaryCard> SummaryCards { get; set; } = new();
        public List<ProductRow> Products { get; set; } = new();
        
        // Filter Lists (Strings for UI compatibility)
        public List<string> Categories { get; set; } = new();
        public List<string> Units { get; set; } = new();
        public List<string> ProductTypes { get; set; } = new();

        // Form Data (Dynamics for ID mapping)
        public List<dynamic> CategoriesList { get; set; } = new();
        public List<dynamic> UnitsList { get; set; } = new();
        public List<dynamic> SuppliersList { get; set; } = new();
        public List<dynamic> WarehousesList { get; set; } = new();
        public List<ProductSupplierViewRow> ProductSuppliers { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Category { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ProductType { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }

        public int FilteredCount { get; set; }
        public int TotalCount { get; set; }

        [BindProperty]
        public ProductCreateDto NewProduct { get; set; } = new();

        [BindProperty]
        public IFormFile? ImageUpload { get; set; }

        [BindProperty]
        public int DeactivateProductID { get; set; }

        [BindProperty]
        public SupplierProductDto SupplierProduct { get; set; } = new();

        [BindProperty]
        public int BlockSupplierProductID { get; set; }

        [BindProperty]
        public ProductStorageRuleSaveDto StorageRule { get; set; } = new();

        [BindProperty]
        public int DeactivateStorageRuleID { get; set; }

        public List<CategoryDetailDto> CategoryDetails { get; set; } = new();
        public List<UnitDetailDto> UnitDetails { get; set; } = new();

        [BindProperty]
        public CategorySaveDto CategoryForm { get; set; } = new();

        [BindProperty]
        public UnitSaveDto UnitForm { get; set; } = new();

        [BindProperty]
        public int ToggleCategoryID { get; set; }

        [BindProperty]
        public int ToggleUnitID { get; set; }

        public bool IsAdmin => _userContext.Role == "Admin";
        public bool CanAddProduct => IsAdmin;
        public bool CanDeactivate => IsAdmin;
        public bool CanManageAttributes => IsAdmin || _userContext.Role == "Manager";
        public bool IsStaff => _userContext.Role == "Warehouse Staff";

        public async Task OnGetAsync(int? productId)
        {
            await LoadDataAsync();
            if (productId.HasValue)
            {
                Products = Products.Where(p => p.ProductID == productId.Value).ToList();
            }
            ApplyFilters();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return Page();
            }

            if (ImageUpload != null)
            {
                // Ensure directory exists
                string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "products");
                Directory.CreateDirectory(uploadsFolder);
                
                // Make a unique filename
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ImageUpload.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageUpload.CopyToAsync(fileStream);
                }
                
                NewProduct.ImageUrl = "/images/products/" + uniqueFileName;
            }

            var result = await _productService.CreateProductAsync(NewProduct);
            
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
            if (DeactivateProductID <= 0)
            {
                ErrorMessage = "Invalid Product ID for deactivation.";
                await LoadDataAsync();
                return Page();
            }

            var result = await _productService.DeactivateProductAsync(DeactivateProductID);

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

        public async Task<IActionResult> OnPostSaveCategoryAsync()
        {
            if (!CanManageAttributes)
            {
                return Forbid();
            }

            var result = await _productService.SaveCategoryAsync(CategoryForm);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage();
            }

            ErrorMessage = result.Message;
            await LoadDataAsync();
            ApplyFilters();
            return Page();
        }

        public async Task<IActionResult> OnPostToggleCategoryStatusAsync()
        {
            if (!CanManageAttributes)
            {
                return Forbid();
            }

            var result = await _productService.ToggleCategoryStatusAsync(ToggleCategoryID);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage();
            }

            ErrorMessage = result.Message;
            await LoadDataAsync();
            ApplyFilters();
            return Page();
        }

        public async Task<IActionResult> OnPostSaveUnitAsync()
        {
            if (!CanManageAttributes)
            {
                return Forbid();
            }

            var result = await _productService.SaveUnitAsync(UnitForm);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage();
            }

            ErrorMessage = result.Message;
            await LoadDataAsync();
            ApplyFilters();
            return Page();
        }

        public async Task<IActionResult> OnPostToggleUnitStatusAsync()
        {
            if (!CanManageAttributes)
            {
                return Forbid();
            }

            var result = await _productService.ToggleUnitStatusAsync(ToggleUnitID);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage();
            }

            ErrorMessage = result.Message;
            await LoadDataAsync();
            ApplyFilters();
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

                // Load Static and Dynamic Filters/Dropdowns
                ProductTypes = new List<string> { "Raw Material", "Finished Goods", "Consumable" };
                CategoriesList = (await _productService.GetCategoriesAsync()).ToList();
                UnitsList = (await _productService.GetUnitsAsync()).ToList();
                SuppliersList = (await _purchaseOrderService.GetActiveSuppliersAsync()).ToList();
                WarehousesList = (await _warehouseService.GetWarehousesAsync()).ToList();
                CategoryDetails = (await _productService.GetCategoriesDetailedAsync()).ToList();
                UnitDetails = (await _productService.GetUnitsDetailedAsync()).ToList();

                Categories = new List<string> { "All Categories" };
                Categories.AddRange(CategoriesList.Select(c => (string)c.Name));

                Units = new List<string> { "All Units" };
                Units.AddRange(UnitsList.Select(u => (string)u.Code));

                // Fetch table data from SQL
                var productsData = await _productService.GetProductMasterAsync();
                var supplierProducts = (await _purchaseOrderService.GetSupplierProductMappingsAsync()).ToList();
                var summaryData = await _productService.GetProductSummaryAsync();

                

                // Map Summary Cards
                SummaryCards = new List<ProductSummaryCard>
                {
                    new ProductSummaryCard { Title = "Total Products", Value = summaryData.TotalProducts.ToString("N0"), Icon = "inventory_2", ColorClass = "primary" },
                    new ProductSummaryCard { Title = "Raw Materials", Value = summaryData.RawMaterials.ToString("N0"), Icon = "account_tree", ColorClass = "info" },
                    new ProductSummaryCard { Title = "Finished Goods", Value = summaryData.FinishedGoods.ToString("N0"), Icon = "check_circle", ColorClass = "success" },
                    new ProductSummaryCard { Title = "Low Stock SKUs", Value = summaryData.LowStockSkus.ToString("N0"), Icon = "warning", ColorClass = "danger" }
                };

                // Map Product Rows
                Products = productsData.Select(p => new ProductRow
                {
                    ProductID = p.ProductID,
                    CategoryID = p.CategoryID,
                    UnitID = p.UnitID,
                    RawStandardCost = p.StandardCost,
                    RawSellingPrice = p.SellingPrice,
                    RawWeight = p.WeightKg,
                    Dimensions = p.Dimensions ?? "",
                    SKU = p.SKU,
                    Name = p.ProductName,
                    Category = p.CategoryName,
                    Type = p.ProductType,
                    Unit = p.UnitCode,
                    ReorderLevel = (int)p.ReorderLevel,
                    StandardCost = $"NPR {p.StandardCost:N0}",
                    SellingPrice = p.SellingPrice > 0 ? $"NPR {p.SellingPrice:N0}" : "-",
                    Weight = $"{p.WeightKg:N2} kg",
                    Status = p.Status,
                    IsLowStock = p.IsLowStock,
                    ImageUrl = p.ImageUrl
                }).ToList();

                ProductSuppliers = supplierProducts.Select(mapping =>
                {
                    var product = productsData.FirstOrDefault(p => p.ProductID == mapping.ProductID);
                    var supplier = SuppliersList.FirstOrDefault(s => s.ID == mapping.SupplierID);
                    return new ProductSupplierViewRow
                    {
                        SupplierProductID = mapping.SupplierProductID,
                        ProductID = mapping.ProductID,
                        SupplierID = mapping.SupplierID,
                        SupplierName = supplier?.Name ?? "",
                        SupplierSKU = mapping.SupplierSKU,
                        LastQuotedPrice = mapping.LastQuotedPrice,
                        LeadTimeDays = mapping.LeadTimeDays,
                        MinOrderQty = mapping.MinOrderQty,
                        PackSize = mapping.PackSize,
                        IsPreferred = mapping.IsPreferred,
                        Status = mapping.Status,
                        SKU = product?.SKU ?? "",
                        ProductName = product?.ProductName ?? ""
                    };
                }).ToList();
                TotalCount = Products.Count;
            }
            catch (Exception)
            {
                ErrorMessage = "Unable to connect to the database. Showing offline mode.";
                Products = new List<ProductRow>();
                ProductSuppliers = new List<ProductSupplierViewRow>();
                TotalCount = 0;
            }
        }

        public async Task<JsonResult> OnGetFloorsAsync(int warehouseId)
        {
            var floors = await _warehouseService.GetFloorsByWarehouseAsync(warehouseId);
            return new JsonResult(floors);
        }

        public async Task<JsonResult> OnGetRoomsAsync(int floorId)
        {
            var rooms = await _warehouseService.GetRoomsByFloorAsync(floorId);
            return new JsonResult(rooms);
        }

        public async Task<JsonResult> OnGetRacksAsync(int roomId)
        {
            var racks = await _warehouseService.GetRacksByRoomAsync(roomId);
            return new JsonResult(racks);
        }

        public async Task<JsonResult> OnGetShelvesAsync(int rackId)
        {
            var shelves = await _warehouseService.GetShelvesByRackAsync(rackId);
            return new JsonResult(shelves);
        }

        public async Task<JsonResult> OnGetStorageRulesAsync(int productId)
        {
            var rules = await _storageRuleService.GetRulesByProductIDAsync(productId);
            return new JsonResult(rules);
        }

        public async Task<JsonResult> OnGetProductInsightsAsync(int productId)
        {
            var insights = await _productService.GetProductInsightsAsync(productId);
            return new JsonResult(insights);
        }

        public async Task<JsonResult> OnGetLocationHierarchyAsync(int? shelfId, int? rackId)
        {
            int? warehouseId = null;
            int? floorId = null;
            int? roomId = null;
            int? targetRackId = rackId;
            int? targetShelfId = shelfId;

            try
            {
                using var connection = new SqlConnection(System.IO.File.Exists("appsettings.json")
                    ? new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetConnectionString("DefaultConnection")
                    : "Server=.\\SQLEXPRESS;Database=FactoryIMS;Trusted_Connection=True;TrustServerCertificate=True;");
                await connection.OpenAsync();

                if (targetShelfId.HasValue && targetShelfId > 0)
                {
                    using var command = new SqlCommand(@"
                        SELECT r.RackID, rm.RoomID, f.FloorID, f.WarehouseID 
                        FROM Shelves s
                        INNER JOIN Racks r ON s.RackID = r.RackID
                        INNER JOIN Rooms rm ON r.RoomID = rm.RoomID
                        INNER JOIN Floors f ON rm.FloorID = f.FloorID
                        WHERE s.ShelfID = @ShelfID", connection);
                    command.Parameters.AddWithValue("@ShelfID", targetShelfId.Value);
                    using var reader = await command.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        targetRackId = reader.GetInt32(0);
                        roomId = reader.GetInt32(1);
                        floorId = reader.GetInt32(2);
                        warehouseId = reader.GetInt32(3);
                    }
                }
                else if (targetRackId.HasValue && targetRackId > 0)
                {
                    using var command = new SqlCommand(@"
                        SELECT rm.RoomID, f.FloorID, f.WarehouseID 
                        FROM Racks r
                        INNER JOIN Rooms rm ON r.RoomID = rm.RoomID
                        INNER JOIN Floors f ON rm.FloorID = f.FloorID
                        WHERE r.RackID = @RackID", connection);
                    command.Parameters.AddWithValue("@RackID", targetRackId.Value);
                    using var reader = await command.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        roomId = reader.GetInt32(0);
                        floorId = reader.GetInt32(1);
                        warehouseId = reader.GetInt32(2);
                    }
                }
            }
            catch (Exception)
            {
                // Fallback / ignore database errors in hierarchy lookup
            }

            return new JsonResult(new { 
                warehouseId = warehouseId, 
                floorId = floorId, 
                roomId = roomId, 
                rackId = targetRackId, 
                shelfId = targetShelfId 
            });
        }

        public async Task<IActionResult> OnPostSaveStorageRuleAsync()
        {
            var result = await _storageRuleService.SaveRuleAsync(StorageRule);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage();
            }
            ErrorMessage = result.Message;
            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeactivateStorageRuleAsync()
        {
            if (DeactivateStorageRuleID <= 0)
            {
                ErrorMessage = "Invalid Storage Rule ID for deactivation.";
                await LoadDataAsync();
                return Page();
            }
            var result = await _storageRuleService.DeactivateRuleAsync(DeactivateStorageRuleID);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage();
            }
            ErrorMessage = result.Message;
            await LoadDataAsync();
            return Page();
        }

        private void ApplyFilters()
        {
            var filtered = Products.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim();
                filtered = filtered.Where(p =>
                    p.SKU.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    p.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(Category) && Category != "All Categories")
            {
                filtered = filtered.Where(p => p.Category == Category);
            }

            if (!string.IsNullOrWhiteSpace(ProductType) && ProductType != "All Types")
            {
                filtered = filtered.Where(p => p.Type == ProductType);
            }

            if (!string.IsNullOrWhiteSpace(Status) && Status != "All Statuses")
            {
                filtered = filtered.Where(p => p.Status == Status);
            }

            Products = filtered.ToList();
            FilteredCount = Products.Count;
        }
    }

    public class ProductSummaryCard
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
    }

    public class ProductRow
    {
        public int ProductID { get; set; }
        public int CategoryID { get; set; }
        public int UnitID { get; set; }
        public decimal RawStandardCost { get; set; }
        public decimal RawSellingPrice { get; set; }
        public decimal RawWeight { get; set; }
        public string Dimensions { get; set; } = string.Empty;

        public string SKU { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int ReorderLevel { get; set; }
        public string StandardCost { get; set; } = string.Empty;
        public string SellingPrice { get; set; } = string.Empty;
        public string Weight { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsLowStock { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }

    public class ProductSupplierViewRow
    {
        public int SupplierProductID { get; set; }
        public int ProductID { get; set; }
        public int SupplierID { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string SupplierSKU { get; set; } = string.Empty;
        public decimal? LastQuotedPrice { get; set; }
        public int LeadTimeDays { get; set; }
        public decimal MinOrderQty { get; set; }
        public decimal PackSize { get; set; }
        public bool IsPreferred { get; set; }
        public string Status { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
    }
}


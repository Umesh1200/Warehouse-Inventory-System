using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using IMS.Services;
using IMS.Models.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace IMS.Pages.Inventory.Products
{
    [Authorize(Policy = "ManagerOrAdmin")]
    public class EditModel : PageModel
    {
        private readonly IProductService _productService;
        private readonly IUserContext _userContext;
        private readonly IWebHostEnvironment _env;

        public EditModel(IProductService productService, IUserContext userContext, IWebHostEnvironment env)
        {
            _productService = productService;
            _userContext = userContext;
            _env = env;
        }

        [BindProperty]
        public ProductUpdateDto Product { get; set; } = new();

        [BindProperty]
        public IFormFile? ImageUpload { get; set; }

        public List<dynamic> CategoriesList { get; set; } = new();
        public List<dynamic> UnitsList { get; set; } = new();
        public List<string> ProductTypes { get; set; } = new();
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

        public bool CanEdit => _userContext.Role == "Admin";
        public bool CanManageAttributes => _userContext.Role == "Admin" || _userContext.Role == "Manager";
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (TempData["SuccessMessage"] != null)
            {
                SuccessMessage = TempData["SuccessMessage"]?.ToString() ?? "";
            }

            var productDto = await _productService.GetProductByIdAsync(id);
            if (productDto == null)
            {
                return NotFound();
            }

            Product = new ProductUpdateDto
            {
                ProductID = productDto.ProductID,
                SKU = productDto.SKU,
                ProductName = productDto.ProductName,
                CategoryID = productDto.CategoryID,
                UnitID = productDto.UnitID,
                ProductType = productDto.ProductType,
                ReorderLevel = productDto.ReorderLevel,
                StandardCost = productDto.StandardCost,
                SellingPrice = productDto.SellingPrice,
                WeightKg = productDto.WeightKg,
                Dimensions = productDto.Dimensions,
                Status = productDto.Status,
                ImageUrl = productDto.ImageUrl
            };

            await LoadDropdownsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!CanEdit)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                return Page();
            }

            if (ImageUpload != null && ImageUpload.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "products");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ImageUpload.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageUpload.CopyToAsync(fileStream);
                }

                Product.ImageUrl = "/images/products/" + uniqueFileName;
            }

            var result = await _productService.UpdateProductAsync(Product);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage("/Inventory/Products");
            }

            ErrorMessage = result.Message;
            await LoadDropdownsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostSaveCategoryAsync(int id)
        {
            if (!CanManageAttributes) return Forbid();

            var result = await _productService.SaveCategoryAsync(CategoryForm);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage(new { id });
            }

            ErrorMessage = result.Message;
            await ReloadProductAndDropdownsAsync(id);
            return Page();
        }

        public async Task<IActionResult> OnPostToggleCategoryStatusAsync(int id)
        {
            if (!CanManageAttributes) return Forbid();

            var result = await _productService.ToggleCategoryStatusAsync(ToggleCategoryID);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage(new { id });
            }

            ErrorMessage = result.Message;
            await ReloadProductAndDropdownsAsync(id);
            return Page();
        }

        public async Task<IActionResult> OnPostSaveUnitAsync(int id)
        {
            if (!CanManageAttributes) return Forbid();

            var result = await _productService.SaveUnitAsync(UnitForm);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage(new { id });
            }

            ErrorMessage = result.Message;
            await ReloadProductAndDropdownsAsync(id);
            return Page();
        }

        public async Task<IActionResult> OnPostToggleUnitStatusAsync(int id)
        {
            if (!CanManageAttributes) return Forbid();

            var result = await _productService.ToggleUnitStatusAsync(ToggleUnitID);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage(new { id });
            }

            ErrorMessage = result.Message;
            await ReloadProductAndDropdownsAsync(id);
            return Page();
        }

        private async Task ReloadProductAndDropdownsAsync(int id)
        {
            var productDto = await _productService.GetProductByIdAsync(id);
            if (productDto != null && Product.ProductID == 0)
            {
                Product = new ProductUpdateDto
                {
                    ProductID = productDto.ProductID,
                    SKU = productDto.SKU,
                    ProductName = productDto.ProductName,
                    CategoryID = productDto.CategoryID,
                    UnitID = productDto.UnitID,
                    ProductType = productDto.ProductType,
                    ReorderLevel = productDto.ReorderLevel,
                    StandardCost = productDto.StandardCost,
                    SellingPrice = productDto.SellingPrice,
                    WeightKg = productDto.WeightKg,
                    Dimensions = productDto.Dimensions,
                    Status = productDto.Status,
                    ImageUrl = productDto.ImageUrl
                };
            }
            await LoadDropdownsAsync();
        }

        private async Task LoadDropdownsAsync()
        {
            ProductTypes = new List<string> { "Raw Material", "Finished Goods", "Consumable" };
            CategoriesList = (await _productService.GetCategoriesAsync()).ToList();
            UnitsList = (await _productService.GetUnitsAsync()).ToList();
            CategoryDetails = (await _productService.GetCategoriesDetailedAsync()).ToList();
            UnitDetails = (await _productService.GetUnitsDetailedAsync()).ToList();
        }
    }
}

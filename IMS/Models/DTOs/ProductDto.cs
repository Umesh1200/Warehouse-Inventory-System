namespace IMS.Models.DTOs
{
    public class ProductDto
    {
        public int ProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int UnitID { get; set; }
        public string UnitCode { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public decimal ReorderLevel { get; set; }
        public decimal StandardCost { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal WeightKg { get; set; }
        public string Dimensions { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public bool IsLowStock { get; set; } // Calculated UI property
    }

    public class ProductSummaryDto
    {
        public int TotalProducts { get; set; }
        public int RawMaterials { get; set; }
        public int FinishedGoods { get; set; }
        public int LowStockSkus { get; set; }
    }

    public class ProductCreateDto
    {
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int CategoryID { get; set; }
        public int UnitID { get; set; }
        public string ProductType { get; set; } = string.Empty;
        public decimal ReorderLevel { get; set; }
        public decimal StandardCost { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal WeightKg { get; set; }
        public string Dimensions { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }

    public class ProductUpdateDto
    {
        public int ProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int CategoryID { get; set; }
        public int UnitID { get; set; }
        public string ProductType { get; set; } = string.Empty;
        public decimal ReorderLevel { get; set; }
        public decimal StandardCost { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal WeightKg { get; set; }
        public string Dimensions { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }

    public class ProductActionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? NewID { get; set; }
    }

    public class ProductInsightsDto
    {
        public List<string> Labels { get; set; } = new();
        public List<decimal> StockLevels { get; set; } = new();
        public List<decimal> StockIn { get; set; } = new();
        public List<decimal> StockOut { get; set; } = new();
        public List<decimal> PurchaseVolume { get; set; } = new();
        public List<decimal> SalesVolume { get; set; } = new();
        public List<decimal> Valuation { get; set; } = new();
    }

    public class CategoryDetailDto
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public int ProductCount { get; set; }
    }

    public class CategorySaveDto
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class UnitDetailDto
    {
        public int UnitID { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public string UnitCode { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public int ProductCount { get; set; }
    }

    public class UnitSaveDto
    {
        public int UnitID { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public string UnitCode { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}


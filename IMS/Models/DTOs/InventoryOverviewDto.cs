namespace IMS.Models.DTOs
{
    public class InventoryOverviewDto
    {
        public int ProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string ShelfCode { get; set; } = string.Empty;
        public string ShelfType { get; set; } = string.Empty;
        public decimal CurrentQty { get; set; }
        public decimal ReservedQty { get; set; }
        public decimal QCHoldQty { get; set; }
        public decimal DamagedQty { get; set; }
        public decimal AvailableQty { get; set; }
        public string StockStatus { get; set; } = string.Empty;
        public decimal ReorderLevel { get; set; }
        public decimal StandardCost { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class InventorySummaryDto
    {
        public decimal TotalStockValue { get; set; }
        public int LowStockCount { get; set; }
        public decimal TotalQCHold { get; set; }
        public decimal TotalDamaged { get; set; }
    }
}

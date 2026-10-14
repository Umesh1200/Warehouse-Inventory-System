using System;
using System.Collections.Generic;

namespace IMS.Models.DTOs
{
    public class ProductLocatorSearchResultDto
    {
        public int ProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public decimal TotalCurrentQty { get; set; }
        public decimal TotalAvailableQty { get; set; }
        public int ShelfCount { get; set; }
    }

    public class ProductShelfLocationDto
    {
        public int WarehouseID { get; set; }
        public string WarehouseCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public int FloorID { get; set; }
        public string FloorCode { get; set; } = string.Empty;
        public string FloorName { get; set; } = string.Empty;
        public int RoomID { get; set; }
        public string RoomCode { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public int RackID { get; set; }
        public string RackCode { get; set; } = string.Empty;
        public string RackName { get; set; } = string.Empty;
        public int ShelfID { get; set; }
        public string ShelfCode { get; set; } = string.Empty;
        public string ShelfName { get; set; } = string.Empty;
        public string ShelfType { get; set; } = string.Empty;
        public decimal ShelfMaxQty { get; set; }
        public decimal ShelfMaxWeightKg { get; set; }
        public decimal ShelfCurrentTotalQty { get; set; }
        public decimal ShelfAvailableCapacity { get; set; }
        public decimal ShelfUtilizationPercentage { get; set; }
        public decimal ProductCurrentQty { get; set; }
        public decimal ProductAvailableQty { get; set; }
        public decimal ProductReservedQty { get; set; }
        public decimal ProductQCHoldQty { get; set; }
        public decimal ProductDamagedQty { get; set; }
        public string ProductStockStatus { get; set; } = string.Empty;
        public DateTime? LastUpdated { get; set; }
        public int TierLevel { get; set; } = 1;
        public int SlotPosition { get; set; } = 1;

        public string HierarchyPath => $"{WarehouseName} > {FloorName} > {RoomName} > {RackName} > {ShelfName}";
    }

    public class ProductLocatorDetailDto
    {
        public int ProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public decimal StandardCost { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal ReorderLevel { get; set; }
        public decimal WeightKg { get; set; }
        public string Dimensions { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public decimal TotalCurrentQty { get; set; }
        public decimal TotalAvailableQty { get; set; }
        public decimal TotalReservedQty { get; set; }
        public decimal TotalQCHoldQty { get; set; }
        public decimal TotalDamagedQty { get; set; }
        public decimal TotalValuation { get; set; }
        public string OverallStockStatus { get; set; } = string.Empty;
        public int ShelfCount { get; set; }
        public int WarehouseCount { get; set; }

        public List<ProductShelfLocationDto> Locations { get; set; } = new();
        public List<StockMovementDto> RecentMovements { get; set; } = new();
    }
}

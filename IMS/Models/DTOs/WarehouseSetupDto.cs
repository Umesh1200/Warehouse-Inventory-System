namespace IMS.Models.DTOs
{
    public class WarehouseSetupDto
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
        public decimal MaxQty { get; set; }
        public decimal MaxWeightKg { get; set; }
        public decimal CurrentQty { get; set; }
        public decimal CurrentWeightKg { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public bool IsActive { get; set; }
        public bool WarehouseActive { get; set; }
        public bool FloorActive { get; set; }
        public bool RoomActive { get; set; }
        public bool RackActive { get; set; }

        // Additional fields for Visualization
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public decimal ReservedQty { get; set; }
        public decimal QCHoldQty { get; set; }
        public decimal DamagedQty { get; set; }
        public decimal AvailableQty { get; set; }
        public decimal ReorderLevel { get; set; }
    }

    public class WarehouseSummaryDto
    {
        public int TotalWarehouses { get; set; }
        public int ActiveShelves { get; set; }
        public int QCHoldShelves { get; set; }
        public int CapacityWarnings { get; set; }
        public decimal TotalCapacity { get; set; }
        public decimal TotalUsed { get; set; }
    }
}

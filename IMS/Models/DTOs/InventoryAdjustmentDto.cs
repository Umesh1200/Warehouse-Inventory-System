using System;
using System.Collections.Generic;

namespace IMS.Models.DTOs
{
    public class InventoryAdjustmentDto
    {
        public int AdjustmentID { get; set; }
        public string AdjustmentNo { get; set; } = string.Empty;
        public string AdjustmentType { get; set; } = string.Empty; // AdjustmentIn, AdjustmentOut
        public string Reason { get; set; } = string.Empty;
        public int RequestedBy { get; set; }
        public string RequestedByName { get; set; } = string.Empty;
        public int? ApprovedBy { get; set; }
        public string ApprovedByName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        
        // Aggregates
        public int ItemCount { get; set; }
        public decimal TotalDifferenceQty { get; set; }
    }

    public class InventoryAdjustmentItemDto
    {
        public int AdjItemID { get; set; }
        public int AdjustmentID { get; set; }
        public int ProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        
        // Location
        public int ShelfID { get; set; }
        public string ShelfCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string FloorName { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string RackName { get; set; } = string.Empty;

        public decimal SystemQty { get; set; }
        public decimal CountedQty { get; set; }
        public decimal DifferenceQty { get; set; }
    }

    public class InventoryAdjustmentSummaryDto
    {
        public int TotalAdjustments { get; set; }
        public int PendingAdjustments { get; set; }
        public int AwaitingApprovalCount { get; set; }
        public int ApprovedCount { get; set; }
        public int PostedCount { get; set; }
    }

    public class AdjustmentCreateDto
    {
        public string? AdjustmentNo { get; set; }
        public string AdjustmentType { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public int RequestedBy { get; set; }
        public List<AdjustmentItemCreateDto> Items { get; set; } = new();
    }

    public class AdjustmentItemCreateDto
    {
        public int ProductID { get; set; }
        public int ShelfID { get; set; }
        public decimal SystemQty { get; set; }
        public decimal CountedQty { get; set; }
        public decimal DifferenceQty => CountedQty - SystemQty;
    }

    public class InventoryAdjustmentRowDto
    {
        public int AdjustmentID { get; set; }
        public string AdjustmentNo { get; set; } = string.Empty;
        public string AdjustmentType { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public int RequestedBy { get; set; }
        public string RequestedByName { get; set; } = string.Empty;
        public int? ApprovedBy { get; set; }
        public string ApprovedByName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        
        // Item details
        public int ProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int ShelfID { get; set; }
        public string ShelfCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public decimal SystemQty { get; set; }
        public decimal CountedQty { get; set; }
        public decimal DifferenceQty { get; set; }
        public int ItemCount { get; set; }
    }

    public class AdjustmentActionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? NewID { get; set; }
    }
}

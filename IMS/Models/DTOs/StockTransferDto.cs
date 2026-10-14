using System;
using System.Collections.Generic;

namespace IMS.Models.DTOs
{
    public class StockTransferDto
    {
        public int TransferID { get; set; }
        public string TransferNo { get; set; } = string.Empty;
        public string TransferType { get; set; } = string.Empty;
        public int RequestedBy { get; set; }
        public string RequestedByName { get; set; } = string.Empty;
        public int? ApprovedBy { get; set; }
        public string ApprovedByName { get; set; } = string.Empty;
        public int? AssignedTo { get; set; }
        public string AssignedToName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public DateTime? CompletionDate { get; set; }
        
        // Aggregates & First Item Details
        public int ItemCount { get; set; }
        public decimal TotalQuantity { get; set; }
        public string FirstProductName { get; set; } = string.Empty;
        public string FirstSKU { get; set; } = string.Empty;
        public string FromWarehouseName { get; set; } = string.Empty;
        public string FromShelfCode { get; set; } = string.Empty;
        public string ToWarehouseName { get; set; } = string.Empty;
        public string ToShelfCode { get; set; } = string.Empty;
    }

    public class StockTransferItemDto
    {
        public int TransferItemID { get; set; }
        public int TransferID { get; set; }
        public int ProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        
        // Source
        public int FromShelfID { get; set; }
        public string FromShelfCode { get; set; } = string.Empty;
        public string FromWarehouseName { get; set; } = string.Empty;
        public string FromFloorName { get; set; } = string.Empty;
        public string FromRoomName { get; set; } = string.Empty;
        public string FromRackName { get; set; } = string.Empty;

        // Destination
        public int ToShelfID { get; set; }
        public string ToShelfCode { get; set; } = string.Empty;
        public string ToWarehouseName { get; set; } = string.Empty;
        public string ToFloorName { get; set; } = string.Empty;
        public string ToRoomName { get; set; } = string.Empty;
        public string ToRackName { get; set; } = string.Empty;

        public decimal Quantity { get; set; }
    }

    public class StockTransferSummaryDto
    {
        public int TotalTransfers { get; set; }
        public int PendingTransfers { get; set; }
        public int AwaitingApprovalCount { get; set; }
        public int InTransitCount { get; set; }
        public int CompletedCount { get; set; }
    }

    public class StockTransferCreateDto
    {
        public string TransferNo { get; set; } = string.Empty;
        public string TransferType { get; set; } = string.Empty;
        public int RequestedBy { get; set; }
        public int? AssignedTo { get; set; }
        public List<StockTransferItemCreateDto> Items { get; set; } = new();
    }

    public class StockTransferItemCreateDto
    {
        public int ProductID { get; set; }
        public int FromShelfID { get; set; }
        public int ToShelfID { get; set; }
        public decimal Quantity { get; set; }
    }

    public class StockTransferActionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? NewID { get; set; }
    }

    public class ShelfCapacityInfoDto
    {
        public int ShelfID { get; set; }
        public string ShelfCode { get; set; } = string.Empty;
        public string ShelfName { get; set; } = string.Empty;
        public string ShelfType { get; set; } = string.Empty;
        public decimal MaxQty { get; set; }
        public decimal MaxWeightKg { get; set; }
        public decimal CurrentQty { get; set; }
        public decimal AvailableQty { get; set; }
        public decimal AvailableWeightKg { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public string FullPath { get; set; } = string.Empty;
        public bool? IsCompatible { get; set; }
        public string RequiredShelfType { get; set; } = string.Empty;
        public string IncompatibilityReason { get; set; } = string.Empty;
    }
}

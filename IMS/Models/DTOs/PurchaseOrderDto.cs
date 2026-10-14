using System;
using System.Collections.Generic;

namespace IMS.Models.DTOs
{
    public class PurchaseOrderDto
    {
        public int POID { get; set; }
        public string PONumber { get; set; } = string.Empty;
        public int SupplierID { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime? ExpectedDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public int CreatedBy { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public int? ApprovedBy { get; set; }
        public string ApprovedByName { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string Carrier { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
        public string RejectionReason { get; set; } = string.Empty;
        public string SupplierPaymentTerms { get; set; } = string.Empty;
        public int SupplierLeadTimeDays { get; set; }
        public string SupplierStatus { get; set; } = "Active";
        public string DepartmentName { get; set; } = "Inventory / Procurement";
        
        // Summaries from items
        public int ItemCount { get; set; }
        public decimal TotalOrderedQty { get; set; }
        public decimal TotalReceivedQty { get; set; }
    }

    public class PurchaseOrderItemDto
    {
        public int POItemID { get; set; }
        public int POID { get; set; }
        public int ProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal OrderedQty { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal? ShippedQty { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal AvailableStock { get; set; }
        public decimal ReorderLevel { get; set; }
        public bool IsLowStock => AvailableStock <= ReorderLevel;
    }

    public class PurchaseOrderSummaryDto
    {
        public int TotalOrders { get; set; }
        public int DraftOrders { get; set; }
        public int PendingConfirmedOrders { get; set; }
        public int InTransitOrders { get; set; }
        public int ProcessingOrders { get; set; } // Received / QC / Putaway
    }

    public class PurchaseOrderCreateDto
    {
        public string? PONumber { get; set; }
        public int SupplierID { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Today;
        public DateTime? ExpectedDate { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public int CreatedBy { get; set; }
        public List<PurchaseOrderItemCreateDto> Items { get; set; } = new();
    }

    public class PurchaseOrderUpdateDto
    {
        public int POID { get; set; }
        public string PONumber { get; set; } = string.Empty;
        public int SupplierID { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? ExpectedDate { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public List<PurchaseOrderItemCreateDto> Items { get; set; } = new();
    }

    public class PurchaseOrderItemCreateDto
    {
        public int ProductID { get; set; }
        public decimal OrderedQty { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => OrderedQty * UnitPrice;
    }

    public class PurchaseOrderActionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? NewID { get; set; }
    }

    public class SupplierProductDto
    {
        public int SupplierProductID { get; set; }
        public int SupplierID { get; set; }
        public int ProductID { get; set; }
        public string SupplierSKU { get; set; } = string.Empty;
        public decimal? LastQuotedPrice { get; set; }
        public int LeadTimeDays { get; set; }
        public decimal MinOrderQty { get; set; }
        public decimal PackSize { get; set; }
        public bool IsPreferred { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}

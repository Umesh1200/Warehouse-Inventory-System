using System;
using System.Collections.Generic;

namespace IMS.Models.DTOs
{
    public class SupplierReturnDto
    {
        public int ReturnID { get; set; }
        public string ReturnNo { get; set; } = string.Empty;
        public int SupplierID { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string RMANumber { get; set; } = string.Empty;
        public DateTime ReturnDate { get; set; }
        public string Carrier { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
        public string CreditStatus { get; set; } = "Pending"; // Pending, Issued, Denied
        public string Status { get; set; } = "Draft"; // Draft, Shipped, Completed, Cancelled
        public string Remarks { get; set; } = string.Empty;
        public string CreditNoteNo { get; set; } = string.Empty;
        public decimal? CreditAmount { get; set; }
        public int? CreatedBy { get; set; }
        public string CreatorName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string RejectionReason { get; set; } = string.Empty;
        public string ReplacementSKU { get; set; } = string.Empty;
        public decimal? ReplacementQty { get; set; }
        public string ReplacementTracking { get; set; } = string.Empty;
    }

    public class SupplierReturnItemDto
    {
        public int ReturnItemID { get; set; }
        public int ReturnID { get; set; }
        public int ProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int? SourceShelfID { get; set; }
        public string SourceShelfCode { get; set; } = string.Empty;
    }

    public class SupplierReturnCreateDto
    {
        public string SupplierID { get; set; } = string.Empty; // Bound as string from option
        public string RMANumber { get; set; } = string.Empty;
        public string Carrier { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public List<SupplierReturnItemCreateDto> Items { get; set; } = new();
    }

    public class SupplierReturnItemCreateDto
    {
        public int ProductID { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int SourceShelfID { get; set; }
    }

    public class SupplierReturnSummaryDto
    {
        public int TotalReturns { get; set; }
        public int PendingCredit { get; set; }
        public int ShippedReturns { get; set; }
        public decimal TotalValue { get; set; }
    }
}

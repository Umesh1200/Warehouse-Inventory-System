using System;

namespace IMS.Models.DTOs
{
    public class QCCheckDto
    {
        public int? QCID { get; set; } // Null for pending QC
        public int? ReceiptID { get; set; }
        public int? FGReceiptID { get; set; }
        public string SourceType { get; set; } = "Purchase"; // "Purchase" or "Production"
        public string PONumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public int ProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal ReceivedQty { get; set; }
        public decimal AcceptedQty { get; set; }
        public decimal RejectedQty { get; set; }
        public decimal DamagedQty { get; set; }
        public int? InspectorID { get; set; }
        public string InspectorName { get; set; } = string.Empty;
        public DateTime? InspectionDate { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ReceiptStatus { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
    }

    public class QCCheckSummaryDto
    {
        public int TotalQCItems { get; set; }
        public int PendingQCCount { get; set; }
        public decimal TotalAcceptedQty { get; set; }
        public decimal TotalRejectedDamagedQty { get; set; }
    }

    public class ReceivingItemForQCDto
    {
        public int? ReceiptID { get; set; }
        public int? FGReceiptID { get; set; }
        public string SourceType { get; set; } = "Purchase";
        public string PONumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal ReceivedQty { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class QCSubmitDto
    {
        public int? ReceiptID { get; set; }
        public int? FGReceiptID { get; set; }
        public string ItemSource { get; set; } = "Purchase"; // "Purchase" or "Production"
        public decimal AcceptedQty { get; set; }
        public decimal RejectedQty { get; set; }
        public decimal DamagedQty { get; set; }
        public string? Remarks { get; set; }
        public int InspectorID { get; set; }
    }

    public class QCActionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? NewID { get; set; }
    }
}

using System;

namespace IMS.Models.DTOs
{
    public class ReceivingDto
    {
        public int? ReceiptID { get; set; } // Null for expected items
        public int POItemID { get; set; }
        public string PONumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public int ProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal OrderedQty { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal DamagedQty { get; set; }
        public decimal RejectedQty { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public int? ReceivedBy { get; set; }
        public string ReceivedByName { get; set; } = string.Empty;
        public DateTime? ReceiptDate { get; set; }
        public DateTime? ExpectedDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
    public class ReceivingSummaryDto
    {
        public int TotalReceipts { get; set; }
        public int QCRequiredCount { get; set; }
        public int AcceptedReceipts { get; set; }
        public decimal TotalDamagedRejected { get; set; }
        public int ExpectedTodayCount { get; set; }
    }

    public class ReceivingCreateDto
    {
        public int POItemID { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal DamagedQty { get; set; }
        public decimal RejectedQty { get; set; }
        public string? BatchNumber { get; set; }
        public int ReceivedBy { get; set; }
    }

    public class PurchaseOrderItemReceivingDto
    {
        public int POItemID { get; set; }
        public int POID { get; set; }
        public string PONumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public int ProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal OrderedQty { get; set; }
        public decimal PreviouslyReceivedQty { get; set; }
        public decimal RemainingQty => OrderedQty - PreviouslyReceivedQty;
    }

    public class ReceivingActionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? NewID { get; set; }
    }
}

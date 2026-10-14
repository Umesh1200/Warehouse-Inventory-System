using System;
using System.Collections.Generic;

namespace IMS.Models.DTOs
{
    public class FinishedGoodsReceiptDto
    {
        public int FGReceiptID { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public int PlanID { get; set; }
        public string PlanNumber { get; set; } = string.Empty;
        public int ProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal ProducedQty { get; set; }
        public decimal AcceptedQty { get; set; }
        public decimal RejectedQty { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public int? ReceivedBy { get; set; }
        public string ReceivedByName { get; set; } = string.Empty;
        public DateTime ReceiptDate { get; set; }
        public string Status { get; set; } = string.Empty;
        
        // Context from Plan/BOM
        public string BOMVersion { get; set; } = string.Empty;
        public string ProductionStatus { get; set; } = string.Empty;
    }

    public class FinishedGoodsReceiptSummaryDto
    {
        public int TotalReceipts { get; set; }
        public int PendingQCCount { get; set; }
        public decimal TotalAcceptedQty { get; set; }
        public decimal TotalRejectedQty { get; set; }
        public int CompletedReceipts { get; set; }
    }

    public class FinishedGoodsReceiptCreateDto
    {
        public string ReceiptNumber { get; set; } = string.Empty;
        public int PlanID { get; set; }
        public int ProductID { get; set; }
        public decimal ProducedQty { get; set; }
        public decimal AcceptedQty { get; set; }
        public decimal RejectedQty { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public int ReceivedBy { get; set; }
    }

    public class ProductionPlanForReceiptDto
    {
        public int PlanID { get; set; }
        public string PlanNumber { get; set; } = string.Empty;
        public int ProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal PlannedQty { get; set; }
        public decimal ProducedQtySoFar { get; set; }
        public decimal RemainingQty => PlannedQty - ProducedQtySoFar;
    }

    public class FinishedGoodsReceiptActionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? NewID { get; set; }
    }
}

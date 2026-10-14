using System;

namespace IMS.Models.DTOs
{
    public class PutawayDto
    {
        public int? TaskID { get; set; } // Null for pending tasks not yet created in PutawayTasks table
        public string TaskNo { get; set; } = string.Empty;
        public int QCID { get; set; }
        public int ProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string SourceType { get; set; } = "Purchase"; // "Purchase" or "Production"
        public string SupplierName { get; set; } = string.Empty;
        public string PONumber { get; set; } = string.Empty;
        public decimal QtyToPutaway { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public int? SuggestedShelfID { get; set; }
        public string SuggestedShelfCode { get; set; } = string.Empty;
        public int? ActualShelfID { get; set; }
        public string ActualShelfCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string FloorName { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string RackName { get; set; } = string.Empty;
        public int? AssignedTo { get; set; }
        public string AssignedToName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? CompletedDate { get; set; }
        public decimal ShelfAvailableCapacity { get; set; }
        public decimal ShelfUtilizationPercentage { get; set; }
    }

    public class PutawaySummaryDto
    {
        public int TotalTasks { get; set; }
        public int PendingPutaway { get; set; }
        public int InProgress { get; set; }
        public int Completed { get; set; }
        public decimal QtyWaitingPutaway { get; set; }
    }

    public class PutawayTaskCreateDto
    {
        public int QCID { get; set; }
        public int ProductID { get; set; }
        public decimal QtyToPutaway { get; set; }
        public int? SuggestedShelfID { get; set; }
        public int? AssignedTo { get; set; }
    }

    public class PutawayCompleteDto
    {
        public int TaskID { get; set; }
        public int ActualShelfID { get; set; }
        public int PerformedBy { get; set; }
    }

    public class QCItemForPutawayDto
    {
        public int QCID { get; set; }
        public int ProductID { get; set; }
        public decimal AcceptedQty { get; set; }
    }

    public class PutawayActionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? NewID { get; set; }
    }

    public class ShelfOptionDto
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ShelfCode { get; set; } = string.Empty;
        public string ShelfType { get; set; } = string.Empty;
        public decimal QuantityAvailable { get; set; }
        public decimal WeightAvailableKg { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public decimal ProjectedUtilizationPercentage { get; set; }
        public bool IsCompatible { get; set; }
        public bool IsRecommended { get; set; }
        public string Warning { get; set; } = string.Empty;
    }
}

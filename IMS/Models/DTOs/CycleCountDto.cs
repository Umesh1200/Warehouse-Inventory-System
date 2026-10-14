using System;
using System.Collections.Generic;

namespace IMS.Models.DTOs
{
    public class CycleCountDto
    {
        public int CountID { get; set; }
        public string CountNo { get; set; } = string.Empty;
        public string CountType { get; set; } = string.Empty;
        public int? AssignedTo { get; set; }
        public string AssignedToName { get; set; } = string.Empty;
        public DateTime? ScheduledDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public int? ProductID { get; set; }
        public int? ShelfID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string ShelfName { get; set; } = string.Empty;
        public decimal SystemQty { get; set; }
        public decimal? CountedQty { get; set; }

        public List<CycleCountItemDto> Items { get; set; } = new();
    }

    public class CycleCountItemDto
    {
        public int CountItemID { get; set; }
        public int CountID { get; set; }
        public int ProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int ShelfID { get; set; }
        public string ShelfCode { get; set; } = string.Empty;
        public decimal SystemQty { get; set; }
        public decimal? CountedQty { get; set; }
        public decimal? Variance => CountedQty.HasValue ? CountedQty.Value - SystemQty : null;
    }

    public class CycleCountSummaryDto
    {
        public int TotalCounts { get; set; }
        public int ScheduledCount { get; set; }
        public int InProgressCount { get; set; }
        public int CompletedCount { get; set; }
        public int VarianceFoundCount { get; set; }
    }

    public class CycleCountCreateDto
    {
        public string CountType { get; set; } = string.Empty;
        public int? AssignedTo { get; set; }
        public DateTime? ScheduledDate { get; set; }
        
        // Scope for the count
        public int? WarehouseID { get; set; }
        public int? FloorID { get; set; }
        public int? ShelfID { get; set; }
        public int? ProductID { get; set; }
    }

    public class CycleCountActionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

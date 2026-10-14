using System;
using System.Collections.Generic;

namespace IMS.Models.DTOs
{
    public class ProductionPlanningDto
    {
        public int PlanID { get; set; }
        public string PlanNumber { get; set; } = string.Empty;
        public int BOMID { get; set; }
        public string BOMVersion { get; set; } = string.Empty;
        public int ProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal PlannedQty { get; set; }
        public decimal ProducedQty { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int? AssignedSupervisor { get; set; }
        public string SupervisorName { get; set; } = string.Empty;
        
        // Summaries
        public int MaterialIssueCount { get; set; }
        public int PendingMaterialIssues { get; set; }
        public int FinishedGoodsReceiptCount { get; set; }
        public decimal TotalAcceptedFinishedQty { get; set; }
    }

    public class ProductionPlanningSummaryDto
    {
        public int TotalPlans { get; set; }
        public int ApprovedPlans { get; set; }
        public int InProductionCount { get; set; }
        public int QCPlansCount { get; set; }
        public int CompletedPlans { get; set; }
        public int PendingMaterialCheckCount { get; set; }
    }

    public class ProductionPlanCreateDto
    {
        public string PlanNumber { get; set; } = string.Empty;
        public int BOMID { get; set; }
        public decimal PlannedQty { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(7);
        public string Priority { get; set; } = "Normal";
        public int AssignedSupervisor { get; set; }
    }

    public class ProductionPlanUpdateDto
    {
        public int PlanID { get; set; }
        public string PlanNumber { get; set; } = string.Empty;
        public int BOMID { get; set; }
        public decimal PlannedQty { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }
        public string Priority { get; set; } = string.Empty;
        public int AssignedSupervisor { get; set; }
    }

    public class ProductionPlanningActionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? NewID { get; set; }
    }

    public class BomInspectionResultDto
    {
        public int BOMID { get; set; }
        public string BOMVersion { get; set; } = string.Empty;
        public int FinishedProductID { get; set; }
        public string FinishedProductSKU { get; set; } = string.Empty;
        public string FinishedProductName { get; set; } = string.Empty;
        public decimal FinishedStandardCost { get; set; }
        public decimal PlannedBatchQty { get; set; }
        public decimal EstimatedBatchMaterialCost { get; set; }
        public int TotalComponents { get; set; }
        public int ShortageCount { get; set; }
        public bool IsReady => ShortageCount == 0;
        public List<BomInspectionItemDto> Components { get; set; } = new();
    }

    public class BomInspectionItemDto
    {
        public int RawMaterialID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string UnitCode { get; set; } = string.Empty;
        public decimal RequiredPerUnit { get; set; }
        public decimal WastagePercent { get; set; }
        public decimal TotalRequiredQty { get; set; }
        public decimal AvailableStock { get; set; }
        public bool IsShortage => TotalRequiredQty > AvailableStock;
        public decimal ShortageQty => IsShortage ? (TotalRequiredQty - AvailableStock) : 0;
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
    }
}

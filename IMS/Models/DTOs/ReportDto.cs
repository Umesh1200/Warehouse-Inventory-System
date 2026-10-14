using System;
using System.Collections.Generic;

namespace IMS.Models.DTOs
{
    public class InventoryReportDto
    {
        public decimal TotalInventoryValue { get; set; }
        public int TotalActiveProducts { get; set; }
        public int LowStockCount { get; set; }
        public decimal TotalQCHoldQty { get; set; }
        public decimal TotalDamagedQty { get; set; }
    }

    public class WarehouseReportDto
    {
        public int CapacityWarningCount { get; set; }
        public int QCHoldShelfCount { get; set; }
        public int EmptyShelfCount { get; set; }
        public decimal AvgUtilization { get; set; }
    }

    public class PurchaseReportDto
    {
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public Dictionary<string, int> StatusCounts { get; set; } = new();
    }

    public class SalesReportDto
    {
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int DailyShipmentCount { get; set; }
        public Dictionary<string, int> StatusCounts { get; set; } = new();
    }

    public class MovementReportDto
    {
        public decimal TotalStockIn { get; set; }
        public decimal TotalStockOut { get; set; }
        public int TransferCount { get; set; }
        public int AdjustmentCount { get; set; }
        public int ProductionIssueCount { get; set; }
        public int ProductionReturnCount { get; set; }
    }

    public class ProductionReportDto
    {
        public int TotalPlans { get; set; }
        public int InProductionCount { get; set; }
        public int PendingMaterialCheckCount { get; set; }
        public int CompletedPlansCount { get; set; }
        public decimal TotalAcceptedFGQty { get; set; }
        public decimal TotalRejectedFGQty { get; set; }
    }

    public class ReportDashboardDto
    {
        public InventoryReportDto Inventory { get; set; } = new();
        public WarehouseReportDto Warehouse { get; set; } = new();
        public PurchaseReportDto Purchase { get; set; } = new();
        public SalesReportDto Sales { get; set; } = new();
        public MovementReportDto Movement { get; set; } = new();
        public ProductionReportDto Production { get; set; } = new();
    }

    public class ReportTableDataDto
    {
        public string ReportKey { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Headers { get; set; } = new();
        public List<List<string>> Rows { get; set; } = new();
        public int TotalCount { get; set; }
        public Dictionary<string, string> SummaryMetrics { get; set; } = new();
        public string GeneratedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public class ReportScheduleDto
    {
        public int ScheduleID { get; set; }
        public string ReportName { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty; // Daily, Weekly, Monthly
        public string Time { get; set; } = "08:00 AM";
        public string Recipients { get; set; } = string.Empty;
        public string Format { get; set; } = "PDF, Excel";
        public bool IsActive { get; set; } = true;
        public string LastSent { get; set; } = "Never";
        public string NextRun { get; set; } = "Tomorrow, 08:00 AM";
    }
}

namespace IMS.Models.DTOs
{
    public class DashboardSummaryDto
    {
        public int TotalActiveProducts { get; set; }
        public int TotalLowStockAlerts { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public int PendingPurchaseOrders { get; set; }
        public int PendingSalesOrders { get; set; }
        public int DailyShipments { get; set; }
    }

    public class DashboardLowStockDto
    {
        public int ProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string ShelfCode { get; set; } = string.Empty;
        public decimal AvailableQty { get; set; }
        public decimal ReorderLevel { get; set; }
        public string StockStatus { get; set; } = string.Empty;
    }

    public class DashboardShelfUtilizationDto
    {
        public string WarehouseName { get; set; } = string.Empty;
        public string FloorName { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string RackName { get; set; } = string.Empty;
        public int ShelfID { get; set; }
        public string ShelfCode { get; set; } = string.Empty;
        public string ShelfType { get; set; } = string.Empty;
        public decimal MaxQty { get; set; }
        public decimal CurrentTotalQty { get; set; }
        public decimal UtilizationPercentage { get; set; }
    }

    public class DashboardMovementDto
    {
        public int MovementID { get; set; }
        public DateTime MovementDate { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string ShelfCode { get; set; } = string.Empty;
        public string MovementType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal BeforeQty { get; set; }
        public decimal AfterQty { get; set; }
        public string ReferenceType { get; set; } = string.Empty;
        public string ReferenceNo { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
    }

    public class AdminDashboardDataDto
    {
        public DashboardSummaryDto Summary { get; set; } = new();
        public List<DashboardLowStockDto> LowStockItems { get; set; } = new();
        public List<DashboardShelfUtilizationDto> ShelfUtilization { get; set; } = new();
        public List<DashboardMovementDto> RecentMovements { get; set; } = new();
        public List<DashboardWarehouseOptionDto> Warehouses { get; set; } = new();
        public List<DashboardStockValueDto> StockValueBreakdown { get; set; } = new();
        public List<DashboardStockValueDto> StockValueByCategory { get; set; } = new();
        public List<DashboardMovementTrendDto> MovementTrend { get; set; } = new();
        
        // Executive Hub Extensions
        public List<DashboardTopProductDto> TopProductsByValue { get; set; } = new();
        public List<DashboardTopProductDto> TopSellingProducts { get; set; } = new();
        public DashboardProcurementFinanceDto FinanceSummary { get; set; } = new();
        public List<DashboardTopSupplierDto> TopSuppliers { get; set; } = new();
        public List<DashboardPendingPipelineDto> InboundPipeline { get; set; } = new();
        public List<DashboardStaffPerformanceDto> StaffPerformance { get; set; } = new();
    }

    public class DashboardWarehouseOptionDto
    {
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
    }

    public class DashboardStockValueDto
    {
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal StockValue { get; set; }
    }

    public class DashboardMovementTrendDto
    {
        public DateTime Date { get; set; }
        public decimal InboundQty { get; set; }
        public decimal OutboundQty { get; set; }
    }

    public class DashboardTopProductDto
    {
        public int ProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public decimal AvailableQty { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }
        public decimal UnitsSold { get; set; }
        public decimal TotalSalesAmount { get; set; }
        public string StockStatus { get; set; } = string.Empty;
    }

    public class DashboardProcurementFinanceDto
    {
        public decimal TotalInventoryCapital { get; set; }
        public decimal MonthlyPOSpend { get; set; }
        public decimal MonthlySalesRevenue { get; set; }
        public decimal PendingPOCommitments { get; set; }
        public decimal PendingSalesReceivables { get; set; }
        public int ActiveSuppliersCount { get; set; }
        public int PendingApprovalTransfers { get; set; }
    }

    public class DashboardTopSupplierDto
    {
        public int SupplierID { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public decimal TotalSpend { get; set; }
        public decimal QCApprovalRate { get; set; }
    }

    public class DashboardPendingPipelineDto
    {
        public int POID { get; set; }
        public string PONumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime? ExpectedDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ItemCount { get; set; }
    }

    public class DashboardStaffPerformanceDto
    {
        public int UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public int TasksCompletedToday { get; set; }
        public decimal UnitsMovedToday { get; set; }
        public string Initials { get; set; } = string.Empty;
        public string AvatarColor { get; set; } = string.Empty;
        public string LastActiveTime { get; set; } = string.Empty;
    }
}

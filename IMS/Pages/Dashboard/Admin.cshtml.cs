using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using IMS.Services;
using IMS.Models.DTOs;
using System.Text.Json;

namespace IMS.Pages.Dashboard
{
    public class AdminModel : PageModel
    {
        private readonly IDashboardService _dashboardService;
        public string ErrorMessage { get; set; } = string.Empty;

        public AdminModel(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public List<KPICard> KPICards { get; set; } = new();
        public List<WarehouseUtilization> UtilizationData { get; set; } = new();
        public List<StockMovement> RecentMovements { get; set; } = new();
        public List<LowStockAlert> LowStockAlerts { get; set; } = new();
        public List<DashboardWarehouseOptionDto> Warehouses { get; set; } = new();
        public List<DashboardStockValueDto> StockValueByCategory { get; set; } = new();
        public List<DashboardTopProductDto> TopProductsByValue { get; set; } = new();
        public List<DashboardTopProductDto> TopSellingProducts { get; set; } = new();
        public DashboardProcurementFinanceDto FinanceSummary { get; set; } = new();
        public List<DashboardTopSupplierDto> TopSuppliers { get; set; } = new();
        public List<DashboardPendingPipelineDto> InboundPipeline { get; set; } = new();
        public List<DashboardStaffPerformanceDto> StaffPerformance { get; set; } = new();
        public string UtilizationChartJson { get; set; } = "{}";
        public string StockValueChartJson { get; set; } = "{}";
        public string StockValueByCategoryChartJson { get; set; } = "{}";
        public string LowStockAlertsJson { get; set; } = "[]";
        public string MovementTrendChartJson { get; set; } = "{}";
        public string FulfillmentGaugeJson { get; set; } = "{}";
        public decimal TotalStockValue { get; set; }
        public int DailyShipmentTarget { get; set; } = 20;

        [BindProperty(SupportsGet = true)]
        public int? WarehouseID { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                var data = await _dashboardService.GetAdminDashboardDataAsync(WarehouseID);
                Warehouses = data.Warehouses;
                TotalStockValue = data.Summary.TotalInventoryValue;
                TopProductsByValue = data.TopProductsByValue;
                TopSellingProducts = data.TopSellingProducts;
                FinanceSummary = data.FinanceSummary;
                TopSuppliers = data.TopSuppliers;
                InboundPipeline = data.InboundPipeline;
                StaffPerformance = data.StaffPerformance;

                // Map KPI Cards
                KPICards = new List<KPICard>
                {
                    new KPICard { Title = "Total Stock Value", Value = $"Rs. {data.Summary.TotalInventoryValue:N0}", Trend = "", TrendUp = true, Subtext = $"Across {data.Summary.TotalActiveProducts} SKUs", Icon = "payments", ColorClass = "primary" },
                    new KPICard { Title = "Low Stock Items", Value = data.Summary.TotalLowStockAlerts.ToString(), Trend = "", TrendUp = false, Subtext = "Require immediate reorder", Icon = "warning", ColorClass = "danger" },
                    new KPICard { Title = "Pending PO Commitments", Value = $"Rs. {data.FinanceSummary.PendingPOCommitments:N0}", Trend = "", TrendUp = false, Subtext = $"{data.Summary.PendingPurchaseOrders} POs in pipeline", Icon = "local_shipping", ColorClass = "info" },
                    new KPICard { Title = "Daily Fulfillment", Value = $"{data.Summary.DailyShipments} Orders", Trend = "", TrendUp = true, Subtext = "Orders fulfilled today", Icon = "outbox", ColorClass = "success" }
                };

                // Map top shelf capacity pressure
                UtilizationData = data.ShelfUtilization.Take(10).Select(u => new WarehouseUtilization
                {
                    Name = $"{u.WarehouseName} / {u.FloorName} / {u.RoomName} / {u.RackName} / {u.ShelfCode}",
                    Percentage = (int)u.UtilizationPercentage,
                    Color = u.UtilizationPercentage > 80 ? "#ef4444" : (u.UtilizationPercentage > 60 ? "#f97316" : "#22c55e")
                }).ToList();

                // Map Stock Movements
                RecentMovements = data.RecentMovements.Select(m => new StockMovement
                {
                    Date = m.MovementDate.ToString("MMM dd, HH:mm"),
                    SKU = m.SKU,
                    Product = m.ProductName,
                    Type = m.MovementType,
                    Quantity = m.Quantity > 0 ? $"+{m.Quantity:N0}" : m.Quantity.ToString("N0"),
                    IsIn = m.MovementType.Contains("In") || m.MovementType == "ProductionReturn" || m.MovementType == "ReturnIn"
                }).ToList();

                // Map Low Stock Alerts
                LowStockAlerts = data.LowStockItems.Select(l => new LowStockAlert
                {
                    SKU = l.SKU,
                    Product = l.ProductName,
                    Quantity = l.AvailableQty.ToString("N0"),
                    Unit = "Units",
                    ReorderPoint = (int)l.ReorderLevel,
                    Status = l.AvailableQty <= 0 ? "Out of Stock" : "Need PO",
                    Severity = l.AvailableQty <= 0 ? "Error" : "Warning"
                }).ToList();

                BuildChartJson(data);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Database error: {ex.Message}" + (ex.InnerException != null ? $" ({ex.InnerException.Message})" : "");
                // Fallback to empty lists to avoid null ref in view
            }
        }

        private void BuildChartJson(AdminDashboardDataDto data)
        {
            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            LowStockAlertsJson = JsonSerializer.Serialize(LowStockAlerts, jsonOptions);
            var today = DateTime.Today;
            var days = Enumerable.Range(0, 7).Select(offset => today.AddDays(-6 + offset)).ToList();

            UtilizationChartJson = JsonSerializer.Serialize(new
            {
                labels = UtilizationData.Select(u => u.Name),
                values = UtilizationData.Select(u => u.Percentage)
            }, jsonOptions);

            // Top 9 SKUs + Others
            var stockValueLabels = data.StockValueBreakdown.Select(s => s.SKU).ToList();
            var stockValueValues = data.StockValueBreakdown.Select(s => s.StockValue).ToList();
            var stockValueProducts = data.StockValueBreakdown.Select(s => s.ProductName).ToList();

            var topValueSum = data.StockValueBreakdown.Sum(s => s.StockValue);
            var othersValue = TotalStockValue - topValueSum;
            if (othersValue > 0.01m)
            {
                stockValueLabels.Add("Others");
                stockValueValues.Add(othersValue);
                stockValueProducts.Add("Other Products");
            }

            StockValueChartJson = JsonSerializer.Serialize(new
            {
                labels = stockValueLabels,
                values = stockValueValues,
                products = stockValueProducts,
                total = TotalStockValue
            }, jsonOptions);

            // Group by Category
            var categoryValueLabels = data.StockValueByCategory.Select(s => s.ProductName).ToList();
            var categoryValueValues = data.StockValueByCategory.Select(s => s.StockValue).ToList();

            StockValueByCategoryChartJson = JsonSerializer.Serialize(new
            {
                labels = categoryValueLabels,
                values = categoryValueValues,
                total = TotalStockValue
            }, jsonOptions);

            var trendByDay = data.MovementTrend.ToDictionary(t => t.Date.Date, t => t);
            MovementTrendChartJson = JsonSerializer.Serialize(new
            {
                labels = days.Select(d => d.ToString("MMM dd")),
                inbound = days.Select(d => trendByDay.TryGetValue(d.Date, out var point) ? point.InboundQty : 0),
                outbound = days.Select(d => trendByDay.TryGetValue(d.Date, out var point) ? point.OutboundQty : 0)
            }, jsonOptions);

            FulfillmentGaugeJson = JsonSerializer.Serialize(new
            {
                value = data.Summary.DailyShipments,
                target = DailyShipmentTarget,
                remaining = Math.Max(DailyShipmentTarget - data.Summary.DailyShipments, 0)
            }, jsonOptions);
        }
    }

    public class KPICard
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Trend { get; set; } = string.Empty;
        public bool TrendUp { get; set; }
        public string Subtext { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
    }

    public class WarehouseUtilization
    {
        public string Name { get; set; } = string.Empty;
        public int Percentage { get; set; }
        public string Color { get; set; } = string.Empty;
    }

    public class StockMovement
    {
        public string Date { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string Product { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Quantity { get; set; } = string.Empty;
        public bool IsIn { get; set; }
    }

    public class LowStockAlert
    {
        public string SKU { get; set; } = string.Empty;
        public string Product { get; set; } = string.Empty;
        public string Quantity { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int ReorderPoint { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty; // Error, Warning
    }
}

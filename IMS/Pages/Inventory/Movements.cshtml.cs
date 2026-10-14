using Microsoft.AspNetCore.Mvc.RazorPages;
using IMS.Services;
using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System;

namespace IMS.Pages.Inventory
{
    public class MovementsModel : PageModel
    {
        private readonly IStockMovementService _movementService;
        public string ErrorMessage { get; set; } = string.Empty;

        public MovementsModel(IStockMovementService movementService)
        {
            _movementService = movementService;
        }

        public List<MovementSummaryCard> SummaryCards { get; set; } = new();
        public List<MovementRow> Movements { get; set; } = new();
        public List<string> MovementTypes { get; set; } = new();
        public List<string> Warehouses { get; set; } = new();
        public string MovementTypeChartJson { get; set; } = "{}";
        public string WarehouseFlowChartJson { get; set; } = "{}";
        public string DailyTrendChartJson { get; set; } = "{}";
        public string TopProductsChartJson { get; set; } = "{}";
        public string MovementDetailsJson { get; set; } = "[]";

        public async Task OnGetAsync()
        {
            try
            {
                // Fetch live data from SQL
                var movementData = (await _movementService.GetRecentMovementsAsync(100)).ToList();
                var trendData = (await _movementService.GetMovementTrendAsync(7)).ToList();
                MovementTypes = new[] { "All Types" }
                    .Concat(movementData.Select(m => m.MovementType).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().OrderBy(t => t))
                    .ToList();
                Warehouses = new[] { "All Warehouses" }
                    .Concat(movementData.Select(m => m.WarehouseName).Where(w => !string.IsNullOrWhiteSpace(w)).Distinct().OrderBy(w => w))
                    .ToList();

                var inboundTypes = new[] { "StockIn", "ReturnIn", "TransferIn", "AdjustmentIn", "ProductionReturn" };
                var outboundTypes = new[] { "StockOut", "Dispatch", "TransferOut", "AdjustmentOut", "DamageOut", "ProductionIssue" };

                // Map Summary Cards
                SummaryCards = new List<MovementSummaryCard>
                {
                    new MovementSummaryCard { Title = "Recent Movements", Value = movementData.Count.ToString("N0"), Icon = "LOG", ColorClass = "primary" },
                    new MovementSummaryCard { Title = "Inbound Units", Value = $"{movementData.Where(m => inboundTypes.Contains(m.MovementType)).Sum(m => Math.Abs(m.Quantity)):N0}", Icon = "IN", ColorClass = "success" },
                    new MovementSummaryCard { Title = "Outbound Units", Value = $"{movementData.Where(m => outboundTypes.Contains(m.MovementType)).Sum(m => Math.Abs(m.Quantity)):N0}", Icon = "OUT", ColorClass = "danger" },
                    new MovementSummaryCard { Title = "Adjustments", Value = movementData.Count(m => m.MovementType.Contains("Adjustment")).ToString("N0"), Icon = "ADJ", ColorClass = "warning" }
                };

                // Map Movement Rows
                Movements = movementData.Select(m => new MovementRow
                {
                    ID = $"MOV-{m.MovementID}",
                    DateTime = m.MovementDate.ToString("MMM dd, HH:mm"),
                    FullDateTime = m.MovementDate.ToString("yyyy-MM-dd HH:mm"),
                    RawDate = m.MovementDate.ToString("yyyy-MM-dd"),
                    Product = m.ProductName,
                    SKU = m.SKU,
                    Warehouse = m.WarehouseName,
                    Shelf = m.ShelfCode,
                    Type = m.MovementType,
                    Qty = m.Quantity > 0 ? $"+{m.Quantity:N0}" : m.Quantity.ToString("N0"),
                    BeforeQty = (int)m.BeforeQty,
                    AfterQty = (int)m.AfterQty,
                    RefType = m.ReferenceType,
                    RefNo = m.ReferenceNo,
                    PerformedBy = m.PerformedBy,
                    Remarks = m.Remarks
                }).ToList();

                BuildInsightData(movementData, trendData);
            }
            catch (Exception)
            {
                ErrorMessage = "Database connection failed. Showing offline mode.";
                Movements = new List<MovementRow>();
                MovementTypes = new List<string> { "All Types" };
                Warehouses = new List<string> { "All Warehouses" };
                BuildInsightData(new List<StockMovementDto>(), new List<StockMovementTrendDto>());
            }
        }

        private void BuildInsightData(List<StockMovementDto> movements, List<StockMovementTrendDto> trendData)
        {
            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var inboundTypes = new[] { "StockIn", "ReturnIn", "TransferIn", "AdjustmentIn", "ProductionReturn" };
            var outboundTypes = new[] { "StockOut", "Dispatch", "TransferOut", "AdjustmentOut", "DamageOut", "ProductionIssue" };

            MovementTypeChartJson = JsonSerializer.Serialize(new
            {
                labels = movements.GroupBy(m => m.MovementType).OrderByDescending(g => g.Count()).Take(6).Select(g => g.Key),
                values = movements.GroupBy(m => m.MovementType).OrderByDescending(g => g.Count()).Take(6).Select(g => g.Count())
            }, jsonOptions);

            var warehouseGroups = movements
                .GroupBy(m => string.IsNullOrWhiteSpace(m.WarehouseName) ? "Unassigned" : m.WarehouseName)
                .OrderByDescending(g => g.Count())
                .Take(6)
                .ToList();

            WarehouseFlowChartJson = JsonSerializer.Serialize(new
            {
                labels = warehouseGroups.Select(g => g.Key),
                inbound = warehouseGroups.Select(g => g.Where(m => inboundTypes.Contains(m.MovementType)).Sum(m => Math.Abs(m.Quantity))),
                outbound = warehouseGroups.Select(g => g.Where(m => outboundTypes.Contains(m.MovementType)).Sum(m => Math.Abs(m.Quantity))),
                net = warehouseGroups.Select(g =>
                    g.Where(m => inboundTypes.Contains(m.MovementType)).Sum(m => Math.Abs(m.Quantity)) -
                    g.Where(m => outboundTypes.Contains(m.MovementType)).Sum(m => Math.Abs(m.Quantity)))
            }, jsonOptions);

            var startDate = DateTime.Today.AddDays(-6);
            var days = Enumerable.Range(0, 7).Select(offset => startDate.AddDays(offset)).ToList();
            var trendByDay = trendData.ToDictionary(t => t.Date.Date, t => t);
            DailyTrendChartJson = JsonSerializer.Serialize(new
            {
                labels = days.Select(d => d.ToString("MMM dd")),
                inbound = days.Select(d => trendByDay.TryGetValue(d.Date, out var point) ? point.InboundQty : 0),
                outbound = days.Select(d => trendByDay.TryGetValue(d.Date, out var point) ? point.OutboundQty : 0)
            }, jsonOptions);

            TopProductsChartJson = JsonSerializer.Serialize(new
            {
                labels = movements
                    .GroupBy(m => m.SKU)
                    .OrderByDescending(g => g.Sum(m => Math.Abs(m.Quantity)))
                    .Take(5)
                    .Select(g => g.Key),
                values = movements
                    .GroupBy(m => m.SKU)
                    .OrderByDescending(g => g.Sum(m => Math.Abs(m.Quantity)))
                    .Take(5)
                    .Select(g => g.Sum(m => Math.Abs(m.Quantity))),
                products = movements
                    .GroupBy(m => m.SKU)
                    .OrderByDescending(g => g.Sum(m => Math.Abs(m.Quantity)))
                    .Take(5)
                    .Select(g => g.First().ProductName)
            }, jsonOptions);

            MovementDetailsJson = JsonSerializer.Serialize(Movements, jsonOptions);
        }
    }

    public class MovementSummaryCard
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
    }

    public class MovementRow
    {
        public string ID { get; set; } = string.Empty;
        public string DateTime { get; set; } = string.Empty;
        public string FullDateTime { get; set; } = string.Empty;
        public string RawDate { get; set; } = string.Empty;
        public string Product { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string Warehouse { get; set; } = string.Empty;
        public string Shelf { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Qty { get; set; } = string.Empty;
        public int BeforeQty { get; set; }
        public int AfterQty { get; set; }
        public string RefType { get; set; } = string.Empty;
        public string RefNo { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
    }
}

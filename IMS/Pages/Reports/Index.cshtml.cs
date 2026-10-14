using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using IMS.Services;
using IMS.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Linq;

namespace IMS.Pages.Reports
{
    public class IndexModel : PageModel
    {
        private readonly IReportService _reportService;
        public string ErrorMessage { get; set; } = string.Empty;

        public IndexModel(IReportService reportService)
        {
            _reportService = reportService;
        }

        public List<ReportSummaryCard> SummaryCards { get; set; } = new();
        public List<ReportItem> Reports { get; set; } = new();
        public List<string> Categories { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                Categories = new List<string> { "All Categories", "Inventory", "Purchase", "Sales", "Warehouse", "QC", "Production" };

                // Fetch live data from SQL
                var data = await _reportService.GetReportDashboardDataAsync();

                // Map Summary Cards
                SummaryCards = new List<ReportSummaryCard>
                {
                    new ReportSummaryCard { Title = "Stock Value", Value = $"Rs. {data.Inventory.TotalInventoryValue:N0}", Icon = "payments", ColorClass = "primary" },
                    new ReportSummaryCard { Title = "Low Stock Items", Value = $"{data.Inventory.LowStockCount} SKUs", Icon = "warning", ColorClass = "danger" },
                    new ReportSummaryCard { Title = "Monthly Movements", Value = $"{data.Movement.TotalStockIn + data.Movement.TotalStockOut:N0}", Icon = "history", ColorClass = "info" },
                    new ReportSummaryCard { Title = "Wh. Utilization", Value = $"{data.Warehouse.AvgUtilization:N0}%", Icon = "warehouse", ColorClass = "success" }
                };

                // Map report definitions with distinct ReportKeys
                Reports = new List<ReportItem>
                {
                    new ReportItem { Key = "current-stock", Name = "Current Stock Report", Category = "Inventory", Description = "Live snapshot of current and available stock levels across all shelves.", Format = "PDF, Excel", Status = "Ready", LastGenerated = "Live" },
                    new ReportItem { Key = "low-stock", Name = "Low Stock Report", Category = "Inventory", Description = $"Identifies items where AvailableQty is below ReorderLevel. Found {data.Inventory.LowStockCount} items.", Format = "PDF, Excel", Status = "Ready", LastGenerated = "Live" },
                    new ReportItem { Key = "warehouse-utilization", Name = "Warehouse Utilization Report", Category = "Warehouse", Description = $"Analysis of space and weight usage. Found {data.Warehouse.CapacityWarningCount} capacity warnings.", Format = "PDF, Excel", Status = "Ready", LastGenerated = "Live" },
                    new ReportItem { Key = "purchase-orders", Name = "Purchase Order Report", Category = "Purchase", Description = $"Summary of all {data.Purchase.TotalOrders} POs by status, supplier, and date range.", Format = "PDF, Excel", Status = "Ready", LastGenerated = "Live" },
                    new ReportItem { Key = "sales-orders", Name = "Sales Order Report", Category = "Sales", Description = $"Fulfillment rates for {data.Sales.TotalOrders} orders. {data.Sales.DailyShipmentCount} dispatches today.", Format = "PDF, Excel", Status = "Ready", LastGenerated = "Live" },
                    new ReportItem { Key = "stock-movement", Name = "Stock Movement Report", Category = "Inventory", Description = $"Detailed log of all movements. Total 30-day Volume: {data.Movement.TotalStockIn + data.Movement.TotalStockOut:N0} units.", Format = "Excel", Status = "Ready", LastGenerated = "Live" },
                    new ReportItem { Key = "inventory-valuation", Name = "Inventory Valuation Report", Category = "Inventory", Description = $"Total asset value based on Qty * Standard Cost: Rs. {data.Inventory.TotalInventoryValue:N0}.", Format = "PDF, Excel", Status = "Ready", LastGenerated = "Live" }
                };
            }
            catch (Exception)
            {
                ErrorMessage = "Database connection failed. Showing offline mode.";
                Reports = new List<ReportItem>();
            }
        }

        public async Task<IActionResult> OnGetReportDataAsync(string reportKey)
        {
            try
            {
                var data = await _reportService.GetReportDataByKeyAsync(reportKey);
                return new JsonResult(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> OnGetReportSchedulesAsync()
        {
            try
            {
                var schedules = await _reportService.GetReportSchedulesAsync();
                return new JsonResult(new { success = true, schedules = schedules });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> OnGetExportCsvAsync(string reportKey)
        {
            try
            {
                var data = await _reportService.GetReportDataByKeyAsync(reportKey);
                var sb = new StringBuilder();

                // Title & Metadata
                sb.AppendLine($"\"{data.Title}\"");
                sb.AppendLine($"\"Category:\",\"{data.Category}\",\"Generated:\",\"{data.GeneratedAt}\"");
                sb.AppendLine();

                // Headers
                sb.AppendLine(string.Join(",", data.Headers.Select(h => $"\"{h.Replace("\"", "\"\"")}\"")));

                // Rows
                foreach (var row in data.Rows)
                {
                    sb.AppendLine(string.Join(",", row.Select(c => $"\"{(c ?? "").Replace("\"", "\"\"")}\"")));
                }

                var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
                string safeFileName = $"{data.ReportKey}-{DateTime.Now:yyyyMMdd-HHmm}.csv";
                return File(bytes, "text/csv", safeFileName);
            }
            catch (Exception)
            {
                return RedirectToPage();
            }
        }
    }

    public class ReportSummaryCard
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
    }

    public class ReportItem
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string LastGenerated { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}

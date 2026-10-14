using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using IMS.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Data;

namespace IMS.Pages.SupplierPortal
{
    public class DashboardModel : PageModel
    {
        private readonly string _connectionString;
        private readonly IUserContext _userContext;

        public string ErrorMessage { get; set; } = string.Empty;

        // KPI stats
        public int OpenPOCount { get; set; }
        public int PendingReturnCount { get; set; }
        public decimal SuppliedValue { get; set; }
        public decimal QCPassRate { get; set; }

        // Mapped lists
        public List<SupplierProductRow> ProvidedProducts { get; set; } = new();
        public List<DashboardReturnRow> ActionableReturns { get; set; } = new();

        // Trends (for SVG charts)
        public List<TrendItem> SuppliedValueTrend { get; set; } = new();
        public List<TrendItem> QCPassRateTrend { get; set; } = new();

        public DashboardModel(IConfiguration configuration, IUserContext userContext)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("Connection string not found.");
            _userContext = userContext;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!_userContext.SupplierID.HasValue)
            {
                return RedirectToPage("/Account/AccessDenied");
            }

            try
            {
                await LoadDashboardDataAsync(_userContext.SupplierID.Value);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load dashboard data: {ex.Message}";
            }

            return Page();
        }

        private async Task LoadDashboardDataAsync(int supplierId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // 1. Fetch Open PO Count
            using (var cmd = new SqlCommand(@"
                SELECT COUNT(*) 
                FROM PurchaseOrders 
                WHERE SupplierID = @SupplierID 
                AND Status IN ('Draft', 'Confirmed', 'InTransit', 'Received', 'QCCheck', 'Putaway')", connection))
            {
                cmd.Parameters.AddWithValue("@SupplierID", supplierId);
                OpenPOCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // 2. Fetch Pending Return Count
            using (var cmd = new SqlCommand(@"
                SELECT COUNT(*) 
                FROM SupplierReturns 
                WHERE SupplierID = @SupplierID 
                AND Status = 'Draft'", connection))
            {
                cmd.Parameters.AddWithValue("@SupplierID", supplierId);
                PendingReturnCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // 3. Fetch Delivered Goods Value (Completed/Received PO items value)
            using (var cmd = new SqlCommand(@"
                SELECT ISNULL(SUM(ri.ReceivedQty * poi.UnitPrice), 0)
                FROM ReceivingItems ri
                INNER JOIN PurchaseOrderItems poi ON ri.POItemID = poi.POItemID
                INNER JOIN PurchaseOrders po ON poi.POID = po.POID
                WHERE po.SupplierID = @SupplierID", connection))
            {
                cmd.Parameters.AddWithValue("@SupplierID", supplierId);
                SuppliedValue = Convert.ToDecimal(await cmd.ExecuteScalarAsync());
            }

            // 4. Fetch Quality Score (QC Pass Rate)
            using (var cmd = new SqlCommand(@"
                SELECT 
                    ISNULL(SUM(qc.AcceptedQty), 0) AS Accepted,
                    ISNULL(SUM(ri.ReceivedQty), 0) AS Received
                FROM QCChecks qc
                INNER JOIN ReceivingItems ri ON qc.ReceiptID = ri.ReceiptID
                INNER JOIN PurchaseOrderItems poi ON ri.POItemID = poi.POItemID
                INNER JOIN PurchaseOrders po ON poi.POID = po.POID
                WHERE po.SupplierID = @SupplierID", connection))
            {
                cmd.Parameters.AddWithValue("@SupplierID", supplierId);
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    decimal accepted = reader.GetDecimal(0);
                    decimal received = reader.GetDecimal(1);
                    QCPassRate = received > 0 ? (accepted / received) * 100 : 100;
                }
            }

            // 5. Fetch Provided Products
            using (var cmd = new SqlCommand(@"
                SELECT 
                    sp.SupplierProductID,
                    p.SKU,
                    p.ProductName,
                    c.CategoryName AS Category,
                    sp.SupplierSKU,
                    ISNULL(sp.LastQuotedPrice, 0) AS LastQuotedPrice,
                    sp.LeadTimeDays,
                    sp.MinOrderQty,
                    sp.IsPreferred,
                    sp.Status
                FROM SupplierProducts sp
                INNER JOIN Products p ON sp.ProductID = p.ProductID
                LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                WHERE sp.SupplierID = @SupplierID
                ORDER BY p.SKU", connection))
            {
                cmd.Parameters.AddWithValue("@SupplierID", supplierId);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    ProvidedProducts.Add(new SupplierProductRow
                    {
                        SupplierProductID = reader.GetInt32(0),
                        SKU = reader.GetString(1),
                        ProductName = reader.GetString(2),
                        Category = reader.GetString(3),
                        SupplierSKU = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        LastQuotedPrice = reader.GetDecimal(5),
                        LeadTimeDays = reader.GetInt32(6),
                        MinOrderQty = reader.GetDecimal(7),
                        IsPreferred = reader.GetBoolean(8),
                        Status = reader.GetString(9)
                    });
                }
            }

            // 6. Fetch Actionable Returns
            using (var cmd = new SqlCommand(@"
                SELECT ReturnID, ReturnNo, CreatedAt, Remarks, Status
                FROM SupplierReturns
                WHERE SupplierID = @SupplierID AND Status = 'Draft'
                ORDER BY CreatedAt DESC", connection))
            {
                cmd.Parameters.AddWithValue("@SupplierID", supplierId);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    ActionableReturns.Add(new DashboardReturnRow
                    {
                        ReturnID = reader.GetInt32(0),
                        ReturnNo = reader.GetString(1),
                        CreatedAt = reader.GetDateTime(2).ToString("MMM dd, yyyy"),
                        Remarks = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        Status = reader.GetString(4)
                    });
                }
            }

            // 7. Supplied Value Trend (Last 6 Months)
            using (var cmd = new SqlCommand(@"
                SELECT 
                    FORMAT(po.OrderDate, 'MMM yyyy') AS MonthLabel,
                    ISNULL(SUM(ri.ReceivedQty * poi.UnitPrice), 0) AS Value,
                    MIN(po.OrderDate) AS MonthDate
                FROM ReceivingItems ri
                INNER JOIN PurchaseOrderItems poi ON ri.POItemID = poi.POItemID
                INNER JOIN PurchaseOrders po ON poi.POID = po.POID
                WHERE po.SupplierID = @SupplierID AND po.OrderDate >= DATEADD(month, -6, GETDATE())
                GROUP BY FORMAT(po.OrderDate, 'MMM yyyy')
                ORDER BY MonthDate ASC", connection))
            {
                cmd.Parameters.AddWithValue("@SupplierID", supplierId);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    SuppliedValueTrend.Add(new TrendItem
                    {
                        MonthLabel = reader.GetString(0),
                        Value = reader.GetDecimal(1)
                    });
                }
            }

            // 8. Quality Trend (Last 6 Months)
            using (var cmd = new SqlCommand(@"
                SELECT 
                    FORMAT(qc.InspectionDate, 'MMM yyyy') AS MonthLabel,
                    ISNULL(SUM(qc.AcceptedQty), 0) AS Accepted,
                    ISNULL(SUM(ri.ReceivedQty), 0) AS Received,
                    MIN(qc.InspectionDate) AS MonthDate
                FROM QCChecks qc
                INNER JOIN ReceivingItems ri ON qc.ReceiptID = ri.ReceiptID
                INNER JOIN PurchaseOrderItems poi ON ri.POItemID = poi.POItemID
                INNER JOIN PurchaseOrders po ON poi.POID = po.POID
                WHERE po.SupplierID = @SupplierID AND qc.InspectionDate >= DATEADD(month, -6, GETDATE())
                GROUP BY FORMAT(qc.InspectionDate, 'MMM yyyy')
                ORDER BY MonthDate ASC", connection))
            {
                cmd.Parameters.AddWithValue("@SupplierID", supplierId);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    decimal accepted = reader.GetDecimal(1);
                    decimal received = reader.GetDecimal(2);
                    SuppliedValueTrend.Add(new TrendItem
                    {
                        MonthLabel = reader.GetString(0),
                        Rate = received > 0 ? (accepted / received) * 100 : 100
                    });
                }
            }

            // Generate trend data fallback if empty (so charts don't render blank)
            if (SuppliedValueTrend.Count == 0)
            {
                var today = DateTime.Today;
                for (int i = 5; i >= 0; i--)
                {
                    SuppliedValueTrend.Add(new TrendItem
                    {
                        MonthLabel = today.AddMonths(-i).ToString("MMM yyyy"),
                        Value = 0,
                        Rate = 100
                    });
                }
            }
        }
    }

    public class TrendItem
    {
        public string MonthLabel { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal Rate { get; set; }
    }

    public class SupplierProductRow
    {
        public int SupplierProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string SupplierSKU { get; set; } = string.Empty;
        public decimal LastQuotedPrice { get; set; }
        public int LeadTimeDays { get; set; }
        public decimal MinOrderQty { get; set; }
        public bool IsPreferred { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class DashboardReturnRow
    {
        public int ReturnID { get; set; }
        public string ReturnNo { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}

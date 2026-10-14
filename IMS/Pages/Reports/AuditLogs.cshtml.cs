using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using IMS.Services;
using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Text;
using System.Linq;

namespace IMS.Pages.Reports
{
    public class AuditLogsModel : PageModel
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogsModel(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        [BindProperty(SupportsGet = true)]
        public AuditLogFilterDto Filter { get; set; } = new();

        public IReadOnlyList<AuditLogDto> AuditLogs { get; set; } = new List<AuditLogDto>();
        public AuditLogSummaryDto Summary { get; set; } = new();

        public IReadOnlyList<DropdownOptionDto> MovementTypes { get; set; } = new List<DropdownOptionDto>();
        public IReadOnlyList<DropdownOptionDto> Products { get; set; } = new List<DropdownOptionDto>();
        public IReadOnlyList<DropdownOptionDto> Users { get; set; } = new List<DropdownOptionDto>();

        public string ErrorMessage { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            try
            {
                await LoadDropdownsAsync();
                AuditLogs = await _auditLogService.GetStockMovementAuditAsync(Filter);
                Summary = await _auditLogService.GetAuditSummaryAsync(Filter);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading audit logs: " + ex.Message;
            }
        }

        public async Task<IActionResult> OnGetExportCsvAsync()
        {
            try
            {
                var logs = await _auditLogService.GetStockMovementAuditAsync(Filter);
                var csv = new StringBuilder();
                csv.AppendLine("MovementID,Date,Type,SKU,Product,Warehouse,Shelf,Quantity,Before,After,RefType,RefNo,User,Remarks");

                foreach (var log in logs)
                {
                    csv.AppendLine($"{log.MovementID},{log.MovementDate:yyyy-MM-dd HH:mm:ss},{EscapeCsv(log.MovementType)},{EscapeCsv(log.SKU)},{EscapeCsv(log.ProductName)},{EscapeCsv(log.WarehouseName)},{EscapeCsv(log.ShelfCode)},{log.Quantity},{log.BeforeQty},{log.AfterQty},{EscapeCsv(log.ReferenceType)},{EscapeCsv(log.ReferenceNo)},{EscapeCsv(log.PerformedBy)},{EscapeCsv(log.Remarks)}");
                }

                var bytes = Encoding.UTF8.GetBytes(csv.ToString());
                return File(bytes, "text/csv", $"AuditLogs_{DateTime.Now:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error exporting CSV: " + ex.Message;
                return RedirectToPage();
            }
        }

        private async Task LoadDropdownsAsync()
        {
            MovementTypes = await _auditLogService.GetMovementTypesAsync();
            Products = await _auditLogService.GetProductsAsync();
            Users = await _auditLogService.GetUsersAsync();
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }
    }
}

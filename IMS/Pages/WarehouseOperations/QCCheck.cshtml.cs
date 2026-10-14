using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using IMS.Services;
using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace IMS.Pages.WarehouseOperations
{
    public class QCCheckModel : PageModel
    {
        private readonly IQCService _qcService;
        private readonly IUserContext _userContext;
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public QCCheckModel(IQCService qcService, IUserContext userContext)
        {
            _qcService = qcService;
            _userContext = userContext;
        }

        public List<QCItemRow> PendingItems { get; set; } = new();
        public List<QCReportCard> Stats { get; set; } = new();

        [BindProperty]
        public QCSubmitDto NewQCResult { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        public async Task<IActionResult> OnPostSubmitQCAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return Page();
            }

            NewQCResult.InspectorID = _userContext.CurrentUserID;

            var result = await _qcService.SubmitQCResultAsync(NewQCResult);
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage();
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadDataAsync();
                return Page();
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                if (TempData["SuccessMessage"] != null)
                {
                    SuccessMessage = TempData["SuccessMessage"]?.ToString() ?? "";
                }

                // Fetch live data from SQL
                var qcData = (await _qcService.GetQCTasksAsync()).ToList();
                var summaryData = await _qcService.GetQCSummaryAsync();

                // Map Summary Stats Cards
                Stats = new List<QCReportCard>
                {
                    new QCReportCard { Title = "Pending QC", Value = $"{summaryData.PendingQCCount} Items", Icon = "pending_actions", ColorClass = "info" },
                    new QCReportCard { Title = "Accepted Total", Value = $"{summaryData.TotalAcceptedQty:N0} Units", Icon = "check_circle", ColorClass = "success" },
                    new QCReportCard { Title = "Rejections", Value = $"{summaryData.TotalRejectedDamagedQty:N0} Units", Icon = "cancel", ColorClass = "danger" },
                    new QCReportCard { Title = "Total QC Items", Value = summaryData.TotalQCItems.ToString(), Icon = "fact_check", ColorClass = "primary" }
                };

                PendingItems = qcData.Select(q => new QCItemRow
                {
                    ReceiptID = q.ReceiptID ?? 0,
                    FGReceiptID = q.FGReceiptID ?? 0,
                    SourceType = q.SourceType,
                    QCID = q.QCID,
                    PONumber = q.PONumber,
                    Supplier = q.SupplierName,
                    SKU = q.SKU,
                    ProductName = q.ProductName,
                    LotNumber = string.IsNullOrWhiteSpace(q.BatchNumber) ? "-" : q.BatchNumber,
                    ReceivedQty = (int)q.ReceivedQty,
                    AcceptedQty = (int)q.AcceptedQty,
                    DamagedQty = (int)q.DamagedQty,
                    RejectedQty = (int)q.RejectedQty,
                    InspectorName = string.IsNullOrWhiteSpace(q.InspectorName) ? "-" : q.InspectorName,
                    InspectionDate = q.InspectionDate?.ToString("yyyy-MM-dd HH:mm") ?? "-",
                    Remarks = q.Remarks,
                    Status = q.Status == "Pending" ? "QC Required" : q.Status,
                    StatusColor = GetStatusColor(q.Status),
                    InspectionDateTime = q.InspectionDate
                }).ToList();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading QC tasks: {ex.Message}";
                PendingItems = new List<QCItemRow>();
            }
        }

        private string GetStatusColor(string status)
        {
            return status switch
            {
                "Accepted" => "success",
                "Rejected" => "danger",
                "Pending" => "warning",
                "QC Required" => "warning",
                _ => "muted"
            };
        }
    }

    public class QCReportCard
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
    }

    public class QCItemRow
    {
        public int ReceiptID { get; set; }
        public int FGReceiptID { get; set; }
        public string SourceType { get; set; } = "Purchase";
        public int? QCID { get; set; }
        public string PONumber { get; set; } = string.Empty;
        public string Supplier { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string LotNumber { get; set; } = string.Empty;
        public int ReceivedQty { get; set; }
        public int AcceptedQty { get; set; }
        public int DamagedQty { get; set; }
        public int RejectedQty { get; set; }
        public string InspectorName { get; set; } = string.Empty;
        public string InspectionDate { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public DateTime? InspectionDateTime { get; set; }
        public bool CanInspect => !QCID.HasValue && (Status == "QC Required" || Status == "Pending QC" || Status == "Pending");
    }
}

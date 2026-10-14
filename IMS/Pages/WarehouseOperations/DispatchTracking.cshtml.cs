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
    public class DispatchTrackingModel : PageModel
    {
        private readonly IDispatchService _dispatchService;
        private readonly IUserContext _userContext;
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public DispatchTrackingModel(IDispatchService dispatchService, IUserContext userContext)
        {
            _dispatchService = dispatchService;
            _userContext = userContext;
        }

        public List<KPICard> SummaryCards { get; set; } = new();
        public List<ReadyToDispatchOrder> PendingDispatches { get; set; } = new();
        public List<DispatchLogItem> RecentDispatches { get; set; } = new();
        public List<string> Carriers { get; set; } = new();

        [BindProperty]
        public DispatchCreateDto NewDispatch { get; set; } = new();

        [BindProperty]
        public int ConfirmDispatchID { get; set; }

        [BindProperty]
        public int DeliveredDispatchID { get; set; }

        [BindProperty]
        public int FailedDispatchID { get; set; }

        public async Task OnGetAsync(int? soid)
        {
            await LoadDataAsync(soid);
        }

        public async Task<IActionResult> OnPostCreateDispatchAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDataAsync(null);
                return Page();
            }

            NewDispatch.DispatchedBy = _userContext.CurrentUserID;

            var result = await _dispatchService.CreateDispatchAsync(NewDispatch);
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage();
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadDataAsync(null);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostConfirmDispatchAsync()
        {
            if (ConfirmDispatchID <= 0)
            {
                ErrorMessage = "Invalid Dispatch ID.";
                await LoadDataAsync(null);
                return Page();
            }

            var result = await _dispatchService.ConfirmDispatchAsync(ConfirmDispatchID, _userContext.CurrentUserID);
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage();
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadDataAsync(null);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostMarkDeliveredAsync()
        {
            if (DeliveredDispatchID <= 0)
            {
                ErrorMessage = "Invalid Dispatch ID.";
                await LoadDataAsync(null);
                return Page();
            }

            var result = await _dispatchService.MarkDeliveredAsync(DeliveredDispatchID);
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage();
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadDataAsync(null);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostMarkFailedAsync()
        {
            if (FailedDispatchID <= 0)
            {
                ErrorMessage = "Invalid Dispatch ID.";
                await LoadDataAsync(null);
                return Page();
            }

            var result = await _dispatchService.MarkFailedAsync(FailedDispatchID);
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage();
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadDataAsync(null);
                return Page();
            }
        }

        private async Task LoadDataAsync(int? soid)
        {
            try
            {
                if (TempData["SuccessMessage"] != null)
                {
                    SuccessMessage = TempData["SuccessMessage"]?.ToString() ?? "";
                }

                Carriers = new List<string> { "Nepal Post", "Pathao Logistics", "Upaya CityCargo", "Internal Fleet", "Speedy Delivery" };

                // Fetch live data from SQL
                var tasksData = (await _dispatchService.GetDispatchTasksAsync()).ToList();
                var summaryData = await _dispatchService.GetDispatchSummaryAsync();

                // Map Summary Cards
                SummaryCards = new List<KPICard>
                {
                    new KPICard { Title = "Ready to Dispatch", Value = summaryData.ReadyToDispatchCount.ToString(), Icon = "inventory", ColorClass = "warning" },
                    new KPICard { Title = "In Transit", Value = summaryData.InTransitCount.ToString(), Icon = "transit_enterexit", ColorClass = "info" },
                    new KPICard { Title = "Delivered Today", Value = summaryData.DeliveredCount.ToString(), Icon = "task_alt", ColorClass = "success" },
                    new KPICard { Title = "Failed Dispatches", Value = summaryData.FailedCount.ToString(), Icon = "error", ColorClass = "danger" }
                };

                // Map Pending Dispatches (Orders that are Ready or already have a record but not confirmed)
                PendingDispatches = tasksData.Where(t => t.Status == "Ready To Dispatch").Select(t => new ReadyToDispatchOrder
                {
                    SOID = t.SOID,
                    PackID = t.PackID ?? 0,
                    OrderID = t.SONumber,
                    DispatchID = t.DispatchID,
                    Destination = t.CustomerName,
                    PackageCount = t.ItemCount,
                    ItemText = $"{t.ItemCount} item(s), {t.TotalOrderedQty:0.##} unit(s)",
                    Priority = t.RequiredDate < DateTime.Today ? "High" : "Standard",
                    Courier = t.CourierName,
                    VehicleNumber = t.VehicleNumber,
                    TrackingNumber = t.TrackingNumber,
                    RequiredDate = t.RequiredDate,
                    TotalUnits = t.TotalOrderedQty,
                    IsSelected = soid.HasValue && t.SOID == soid.Value
                }).ToList();

                if (PendingDispatches.Any())
                {
                    if (!PendingDispatches.Any(p => p.IsSelected))
                    {
                        PendingDispatches.First().IsSelected = true;
                    }
                }

                // Map Recent Dispatches (Log)
                RecentDispatches = tasksData.Where(t => t.DispatchID.HasValue).Select(t => new DispatchLogItem
                {
                    DispatchID = t.DispatchID ?? 0,
                    SOID = t.SOID,
                    OrderID = t.SONumber,
                    Status = t.Status,
                    Courier = t.CourierName,
                    VehicleNumber = t.VehicleNumber,
                    TrackingNumber = t.TrackingNumber,
                    DispatchTime = t.DispatchDate?.ToString("MMM dd, HH:mm") ?? "-",
                    ColorClass = GetStatusColor(t.Status)
                }).Take(20).ToList();
            }
            catch (Exception)
            {
                ErrorMessage = "Database connection failed. Showing offline mode.";
                PendingDispatches = new List<ReadyToDispatchOrder>();
                RecentDispatches = new List<DispatchLogItem>();
            }
        }

        private string GetStatusColor(string status)
        {
            return status switch
            {
                "Delivered" => "primary",
                "Ready To Dispatch" => "warning",
                "Dispatched" => "success",
                "InTransit" => "info",
                "Failed" => "danger",
                _ => "muted"
            };
        }
    }

    public class KPICard
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
    }

    public class ReadyToDispatchOrder
    {
        public int SOID { get; set; }
        public int PackID { get; set; }
        public int? DispatchID { get; set; }
        public string OrderID { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public int PackageCount { get; set; }
        public string ItemText { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Courier { get; set; } = string.Empty;
        public string VehicleNumber { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
        public DateTime? RequiredDate { get; set; }
        public decimal TotalUnits { get; set; }
        public bool IsSelected { get; set; }
    }

    public class DispatchLogItem
    {
        public int DispatchID { get; set; }
        public int SOID { get; set; }
        public string OrderID { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Courier { get; set; } = string.Empty;
        public string VehicleNumber { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
        public string DispatchTime { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using IMS.Services;
using IMS.Models.DTOs;

namespace IMS.Pages.Dashboard
{
    public class StaffModel : PageModel
    {
        private readonly IUserContext _userContext;
        private readonly IPutawayService _putawayService;
        private readonly IPickingPackingService _pickingService;
        private readonly IStockMovementService _stockMovementService;
        private readonly IUserRoleService _userRoleService;
        private readonly ICycleCountService _cycleCountService;
        private readonly IStockTransferService _transferService;

        public StaffModel(
            IUserContext userContext, 
            IPutawayService putawayService, 
            IPickingPackingService pickingService, 
            IStockMovementService stockMovementService, 
            IUserRoleService userRoleService, 
            ICycleCountService cycleCountService,
            IStockTransferService transferService)
        {
            _userContext = userContext;
            _putawayService = putawayService;
            _pickingService = pickingService;
            _stockMovementService = stockMovementService;
            _userRoleService = userRoleService;
            _cycleCountService = cycleCountService;
            _transferService = transferService;
        }

        public string StaffName { get; set; } = string.Empty;
        public List<KPICard> KPICards { get; set; } = new();
        public List<TaskRow> CurrentTasks { get; set; } = new();
        
        public List<ActivityRow> TeamActivities { get; set; } = new();
        
        // For filters dropdown
        public List<string> FilterUsers { get; set; } = new();
        public List<string> FilterActionTypes { get; set; } = new();

        public async Task OnGetAsync()
        {
            StaffName = _userContext.FullName;
            int userId = _userContext.CurrentUserID;

            IEnumerable<PutawayDto> putawayTasks = new List<PutawayDto>();
            IEnumerable<PickingPackingDto> pickTasks = new List<PickingPackingDto>();
            IEnumerable<CycleCountDto> cycleCounts = new List<CycleCountDto>();
            IEnumerable<StockTransferDto> transfers = new List<StockTransferDto>();
            IEnumerable<StockMovementDto> recentMovements = new List<StockMovementDto>();

            try { putawayTasks = await _putawayService.GetPutawayTasksAsync(); } catch { }
            try { pickTasks = await _pickingService.GetPickPackTasksAsync(); } catch { }
            try { cycleCounts = await _cycleCountService.GetCycleCountsAsync(); } catch { }
            try { transfers = await _transferService.GetTransfersAsync(); } catch { }
            try { recentMovements = await _stockMovementService.GetRecentMovementsAsync(500); } catch { }

            var myPutaways = putawayTasks
                .Where(t => t.AssignedTo == userId && t.Status != "Completed")
                .Select(t => new TaskRow
                {
                    TaskId = t.TaskID ?? 0,
                    TaskType = "Putaway",
                    Name = "Putaway Task",
                    Product = t.ProductName,
                    BatchNo = string.IsNullOrEmpty(t.PONumber) ? "-" : t.PONumber,
                    LocationDetail = string.IsNullOrEmpty(t.SuggestedShelfCode) ? "-" : $"Target: {t.SuggestedShelfCode}",
                    Duration = "Active",
                    Status = t.Status,
                    StatusClass = GetStatusClass(t.Status),
                    ActionUrl = "/WarehouseOperations/Putaway"
                });

            var myPicks = pickTasks
                .Where(t => t.AssignedTo == userId && t.PickStatus != "Completed" && t.PackStatus != "Packed")
                .Select(t => new TaskRow
                {
                    TaskId = t.PickID,
                    TaskType = "Picking",
                    Name = "Picking Task",
                    Product = "Order: " + t.SONumber,
                    BatchNo = t.CustomerName, 
                    LocationDetail = $"{t.ItemCount} items ({t.TotalOrderedQty:N0} qty)",
                    Duration = "Active",
                    Status = t.PickStatus,
                    StatusClass = GetStatusClass(t.PickStatus),
                    ActionUrl = "/WarehouseOperations/PickingPacking"
                });

            var myCycleCounts = cycleCounts
                .Where(t => t.AssignedTo == userId && t.Status != "Completed")
                .Select(t => new TaskRow
                {
                    TaskId = t.CountID,
                    TaskType = "CycleCount",
                    Name = "Cycle Count",
                    Product = string.IsNullOrEmpty(t.ProductName) ? t.CountType : t.ProductName,
                    BatchNo = t.CountNo,
                    LocationDetail = string.IsNullOrEmpty(t.ShelfName) ? t.CountType : t.ShelfName,
                    Duration = "Active",
                    Status = t.Status,
                    StatusClass = GetStatusClass(t.Status),
                    ActionUrl = "/Inventory/CycleCounts"
                });

            // Include Approved / In-Transit Stock Transfers as actionable physical move tasks
            var myTransfers = transfers
                .Where(t => (t.Status == "Approved" || t.Status == "In Transit") && (!t.AssignedTo.HasValue || t.AssignedTo == userId))
                .Select(t => new TaskRow
                {
                    TaskId = t.TransferID,
                    TaskType = "Transfer",
                    Name = "Stock Transfer",
                    Product = !string.IsNullOrEmpty(t.FirstProductName) ? t.FirstProductName : "Inventory Items",
                    BatchNo = t.TransferNo,
                    LocationDetail = $"{t.FromShelfCode} → {t.ToShelfCode}",
                    Duration = t.AssignedTo == userId ? "Assigned to You" : "Open to Floor",
                    Status = t.Status,
                    StatusClass = GetStatusClass(t.Status),
                    ActionUrl = "/Inventory/Transfers"
                });

            CurrentTasks = myPutaways.Concat(myPicks).Concat(myCycleCounts).Concat(myTransfers).ToList();

            var todayMovements = recentMovements
                .Where(m => m.MovementDate.Date == DateTime.Today && m.PerformedBy == StaffName)
                .ToList();

            int completedTasksToday = todayMovements.Count; 
            int pendingTasks = CurrentTasks.Count;
            decimal unitsHandled = todayMovements.Sum(m => Math.Abs(m.Quantity));

            KPICards = new List<KPICard>
            {
                new KPICard { Title = "TASKS COMPLETED TODAY", Value = completedTasksToday.ToString(), Subtext = "(Stock Movements)", Icon = "task_alt", ColorClass = "success" },
                new KPICard { Title = "MY PENDING TASKS", Value = pendingTasks.ToString(), Subtext = "(In Queue)", Icon = "pending_actions", ColorClass = "warning" },
                new KPICard { Title = "UNITS HANDLED TODAY", Value = unitsHandled.ToString("N0"), Subtext = "(Total Qty)", Icon = "inventory_2", ColorClass = "primary" }
            };

            try {
                var userData = await _userRoleService.GetUserManagementDataAsync();
                FilterUsers = userData.Users.Select(u => u.FullName).Distinct().OrderBy(u => u).ToList();
            } catch {
                FilterUsers = recentMovements.Where(m => !string.IsNullOrEmpty(m.PerformedBy)).Select(m => m.PerformedBy).Distinct().OrderBy(u => u).ToList();
            }
            FilterActionTypes = recentMovements.Select(m => m.MovementType).Distinct().OrderBy(t => t).ToList();

            TeamActivities = recentMovements.Where(m => m.MovementDate.Date == DateTime.Today).Take(10).Select(m => MapActivityRow(m)).ToList();
        }

        public async Task<IActionResult> OnPostCompleteTransferAsync(int transferId)
        {
            if (transferId <= 0)
            {
                TempData["ErrorMessage"] = "Invalid Transfer ID.";
                return RedirectToPage();
            }

            var result = await _transferService.PostTransferAsync(transferId, _userContext.CurrentUserID);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetActivitiesAsync(int skip = 0, int take = 10, string timeFilter = "All", string userFilter = "All", string actionFilter = "All")
        {
            var recentMovements = await _stockMovementService.GetRecentMovementsAsync(500);

            var query = recentMovements.AsEnumerable();

            if (timeFilter != "All")
            {
                var today = DateTime.Today;
                if (timeFilter == "Today") query = query.Where(m => m.MovementDate.Date == today);
                else if (timeFilter == "Yesterday") query = query.Where(m => m.MovementDate.Date == today.AddDays(-1));
                else if (timeFilter == "Last7Days") query = query.Where(m => m.MovementDate.Date >= today.AddDays(-7));
            }

            if (userFilter != "All" && !string.IsNullOrEmpty(userFilter))
            {
                query = query.Where(m => m.PerformedBy == userFilter);
            }

            if (actionFilter != "All" && !string.IsNullOrEmpty(actionFilter))
            {
                query = query.Where(m => m.MovementType == actionFilter);
            }

            var totalMatches = query.Count();
            var pagedActivities = query
                .Skip(skip)
                .Take(take)
                .Select(m => MapActivityRow(m))
                .ToList();

            return new JsonResult(new { 
                activities = pagedActivities, 
                hasMore = (skip + take) < totalMatches 
            });
        }

        private ActivityRow MapActivityRow(StockMovementDto m)
        {
            string initials = GetInitials(m.PerformedBy);
            string avatarColor = GetAvatarColor(m.PerformedBy);
            string actionColorClass = m.MovementType switch
            {
                "Receive" or "ReturnIn" or "TransferIn" or "AdjustmentIn" => "action-in",
                "Dispatch" or "DamageOut" or "TransferOut" or "AdjustmentOut" => "action-out",
                "Reserve" or "Unreserve" => "action-reserve",
                _ => "action-neutral"
            };

            return new ActivityRow
            {
                Avatar = initials,
                AvatarColor = avatarColor,
                Text = $"{m.PerformedBy} performed {m.MovementType} on {m.ProductName} ({m.Quantity})",
                Time = GetRelativeTime(m.MovementDate),
                ActionType = m.MovementType,
                ActionColorClass = actionColorClass
            };
        }

        private string GetStatusClass(string status)
        {
            return status?.ToLower() switch
            {
                "completed" or "approved" or "packed" => "badge-success",
                "pending" or "in progress" or "in transit" => "badge-warning",
                "cancelled" or "rejected" => "badge-danger",
                _ => "badge-neutral"
            };
        }

        private string GetInitials(string name)
        {
            if (string.IsNullOrEmpty(name)) return "U";
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2) return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            return name.Substring(0, Math.Min(2, name.Length)).ToUpper();
        }

        private string GetAvatarColor(string name)
        {
            if (string.IsNullOrEmpty(name)) return "#94a3b8"; 
            int hash = name.GetHashCode();
            var colors = new[] { "#8b5cf6", "#14b8a6", "#f59e0b", "#ef4444", "#3b82f6", "#10b981", "#6366f1" };
            return colors[Math.Abs(hash) % colors.Length];
        }

        private string GetRelativeTime(DateTime date)
        {
            var span = DateTime.Now - date;
            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} min ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} hr ago";
            return $"{(int)span.TotalDays} days ago";
        }

        public class KPICard
        {
            public string Title { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
            public string Subtext { get; set; } = string.Empty;
            public string Icon { get; set; } = string.Empty;
            public string ColorClass { get; set; } = string.Empty;
        }

        public class TaskRow
        {
            public int TaskId { get; set; }
            public string TaskType { get; set; } = string.Empty; // Putaway, Picking, CycleCount, Transfer
            public string Name { get; set; } = string.Empty;
            public string Product { get; set; } = string.Empty;
            public string BatchNo { get; set; } = string.Empty;
            public string LocationDetail { get; set; } = string.Empty;
            public string Duration { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string StatusClass { get; set; } = string.Empty;
            public string ActionUrl { get; set; } = string.Empty;
        }

        public class ActivityRow
        {
            public string Avatar { get; set; } = string.Empty;
            public string AvatarColor { get; set; } = string.Empty;
            public string Text { get; set; } = string.Empty;
            public string Time { get; set; } = string.Empty;
            public string ActionType { get; set; } = string.Empty;
            public string ActionColorClass { get; set; } = string.Empty;
        }
    }
}

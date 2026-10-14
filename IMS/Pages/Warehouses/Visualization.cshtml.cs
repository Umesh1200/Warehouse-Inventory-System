using IMS.Models.DTOs;
using IMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace IMS.Pages.Warehouses
{
    public class VisualizationModel : PageModel
    {
        private readonly IWarehouseService _warehouseService;

        public VisualizationModel(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }

        [BindProperty(SupportsGet = true)]
        public int? WarehouseId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FloorId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? RoomId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? RackId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ShelfId { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;
        public List<WarehouseDropdownOption> Warehouses { get; set; } = new();
        public WarehouseHeaderInfo CurrentWarehouse { get; set; } = new();
        public List<FloorDropdownOption> Floors { get; set; } = new();
        public List<VisualizationSummaryCard> SummaryCards { get; set; } = new();
        public List<RoomLayoutItem> Rooms { get; set; } = new();
        public List<RackOptionItem> AvailableRacks { get; set; } = new();
        public RackElevationViewModel ActiveRackElevation { get; set; } = new();
        public ShelfDetails SelectedShelf { get; set; } = new();
        public List<RoomUtilizationSummaryItem> RoomUtilizationSummary { get; set; } = new();
        public List<RackUtilizationTableItem> RackUtilizationTable { get; set; } = new();
        public List<LowStockShelfItem> LowStockShelves { get; set; } = new();
        public string VisualizationDataJson { get; set; } = "{}";
        public DateTime LastUpdated { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                LastUpdated = DateTime.Now;

                // 1. Fetch complete warehouse hierarchy from repository
                var vizData = (await _warehouseService.GetWarehouseVisualizationAsync()).ToList();
                if (!vizData.Any())
                {
                    ErrorMessage = "No warehouse data found. Please configure warehouse storage in Warehouse Setup.";
                    return;
                }

                // 2. Fetch available warehouses list
                var rawWarehouses = (await _warehouseService.GetWarehousesAsync()).ToList();
                Warehouses = rawWarehouses.Select(w => new WarehouseDropdownOption
                {
                    Id = (int)w.ID,
                    Name = (string)w.Name,
                    Code = (string)w.Code
                }).ToList();

                // If no WarehouseId selected, pick the first warehouse that has actual shelves
                if (!WarehouseId.HasValue || !Warehouses.Any(w => w.Id == WarehouseId.Value))
                {
                    var whWithShelves = vizData.FirstOrDefault(v => v.ShelfID > 0)?.WarehouseID;
                    WarehouseId = whWithShelves ?? (vizData.FirstOrDefault()?.WarehouseID ?? Warehouses.FirstOrDefault()?.Id ?? 1);
                }

                // Filter vizData for current warehouse
                var warehouseData = vizData.Where(v => v.WarehouseID == WarehouseId.Value).ToList();
                if (!warehouseData.Any())
                {
                    warehouseData = vizData;
                    WarehouseId = vizData.First().WarehouseID;
                }

                var firstRecord = warehouseData.First();

                // 3. Populate Floors dropdown (only valid floors)
                Floors = warehouseData
                    .Where(s => s.FloorID > 0)
                    .GroupBy(s => s.FloorID)
                    .Select(g =>
                    {
                        var f = g.First();
                        return new FloorDropdownOption
                        {
                            Id = f.FloorID,
                            Code = f.FloorCode,
                            Name = string.IsNullOrWhiteSpace(f.FloorName) ? f.FloorCode : f.FloorName,
                            ShelfCount = g.Count(s => s.ShelfID > 0)
                        };
                    })
                    .OrderBy(f => f.Code)
                    .ToList();

                if (FloorId.HasValue && !Floors.Any(f => f.Id == FloorId.Value))
                {
                    FloorId = null;
                }

                var filteredData = FloorId.HasValue
                    ? warehouseData.Where(s => s.FloorID == FloorId.Value).ToList()
                    : warehouseData;

                // Filter actual existing entities (excluding outer join nulls/zeros)
                var actualShelves = warehouseData.Where(s => s.ShelfID > 0).ToList();
                var filteredActualShelves = filteredData.Where(s => s.ShelfID > 0).ToList();

                // 4. Warehouse Summary / Header Stats
                decimal totalCap = actualShelves.Sum(s => s.MaxQty);
                decimal usedCap = actualShelves.Sum(s => s.CurrentQty);
                int overallUtil = totalCap > 0 ? (int)Math.Round((usedCap / totalCap) * 100) : 0;
                int totalFloors = warehouseData.Where(s => s.FloorID > 0).Select(s => s.FloorID).Distinct().Count();
                int totalRooms = warehouseData.Where(s => s.RoomID > 0).Select(s => s.RoomID).Distinct().Count();
                int totalRacks = warehouseData.Where(s => s.RackID > 0).Select(s => s.RackID).Distinct().Count();
                int totalShelvesCount = actualShelves.Count;
                int usedShelvesCount = actualShelves.Count(s => s.CurrentQty > 0);

                CurrentWarehouse = new WarehouseHeaderInfo
                {
                    Id = firstRecord.WarehouseID,
                    Code = firstRecord.WarehouseCode,
                    Name = firstRecord.WarehouseName,
                    TotalCapacity = totalCap,
                    UsedCapacity = usedCap,
                    UtilizationPercentage = overallUtil,
                    TotalFloors = totalFloors,
                    TotalRooms = totalRooms,
                    TotalRacks = totalRacks,
                    TotalShelves = totalShelvesCount,
                    UsedShelves = usedShelvesCount
                };

                // Top Stat Cards matching Stitch
                SummaryCards = new List<VisualizationSummaryCard>
                {
                    new()
                    {
                        Title = "Warehouse Capacity",
                        Value = $"{overallUtil}%",
                        Subtext = $"Used: {overallUtil}% ({usedCap:N0} / {totalCap:N0} units)",
                        Icon = "inventory_2",
                        ColorClass = "info",
                        IsCapacityCard = true,
                        Percentage = Math.Min(overallUtil, 100)
                    },
                    new()
                    {
                        Title = "Floors & Rooms",
                        Value = $"{totalFloors} Floors / {totalRooms} Rooms",
                        Subtext = "Physical Storage Areas",
                        Icon = "grid_view",
                        ColorClass = "primary"
                    },
                    new()
                    {
                        Title = "Racks",
                        Value = totalRacks.ToString("N0"),
                        Subtext = "Multi-Tier Rack Bays",
                        Icon = "shelves",
                        ColorClass = "warning"
                    },
                    new()
                    {
                        Title = "Active Shelves",
                        Value = $"{usedShelvesCount} / {totalShelvesCount}",
                        Subtext = $"{Math.Max(0, totalShelvesCount - usedShelvesCount)} Shelves Available",
                        Icon = "view_in_ar",
                        ColorClass = "success"
                    }
                };

                // 5. Room Layout Grid (Left Panel in Stitch)
                // Filter ONLY rooms where RoomID > 0
                Rooms = filteredData
                    .Where(s => s.RoomID > 0)
                    .GroupBy(s => s.RoomID)
                    .Select(g =>
                    {
                        var rm = g.First();
                        var roomShelves = g.Where(s => s.ShelfID > 0).ToList();
                        decimal rmMax = roomShelves.Sum(s => s.MaxQty);
                        decimal rmUsed = roomShelves.Sum(s => s.CurrentQty);
                        int rmUtil = rmMax > 0 ? (int)Math.Round((rmUsed / rmMax) * 100) : 0;
                        int usedInRoom = roomShelves.Count(s => s.CurrentQty > 0);

                        return new RoomLayoutItem
                        {
                            RoomId = rm.RoomID,
                            RoomCode = rm.RoomCode,
                            RoomName = string.IsNullOrWhiteSpace(rm.RoomName) ? rm.RoomCode : rm.RoomName,
                            FloorId = rm.FloorID,
                            FloorCode = rm.FloorCode,
                            FloorName = rm.FloorName,
                            TotalShelves = roomShelves.Count,
                            UsedShelves = usedInRoom,
                            UtilizationPercentage = rmUtil,
                            StatusColor = GetStatusColor(rmUtil),
                            HasRawMaterial = roomShelves.Any(s => string.Equals(s.ShelfType, "Raw Material", StringComparison.OrdinalIgnoreCase)),
                            HasFinishedGoods = roomShelves.Any(s => string.Equals(s.ShelfType, "Finished Goods", StringComparison.OrdinalIgnoreCase)),
                            HasQCHold = roomShelves.Any(s => string.Equals(s.ShelfType, "QC Hold", StringComparison.OrdinalIgnoreCase))
                        };
                    })
                    .OrderBy(r => r.RoomCode)
                    .ToList();

                // Select active Room: Prioritize rooms with shelves/racks
                if (!RoomId.HasValue || !Rooms.Any(r => r.RoomId == RoomId.Value))
                {
                    RoomId = Rooms.FirstOrDefault(r => r.TotalShelves > 0)?.RoomId ?? Rooms.FirstOrDefault()?.RoomId;
                }

                // 6. Build ALL Racks across the entire warehouse (so JS room switching works instantly)
                var allWarehouseRacks = warehouseData
                    .Where(s => s.RackID > 0)
                    .GroupBy(s => s.RackID)
                    .Select(g =>
                    {
                        var rk = g.First();
                        var rackShelves = g.Where(s => s.ShelfID > 0).ToList();
                        decimal rkMax = rackShelves.Sum(s => s.MaxQty);
                        decimal rkUsed = rackShelves.Sum(s => s.CurrentQty);
                        int rkUtil = rkMax > 0 ? (int)Math.Round((rkUsed / rkMax) * 100) : 0;

                        return new RackOptionItem
                        {
                            RackId = rk.RackID,
                            RackCode = rk.RackCode,
                            RackName = string.IsNullOrWhiteSpace(rk.RackName) ? rk.RackCode : rk.RackName,
                            RoomId = rk.RoomID,
                            RoomCode = rk.RoomCode,
                            TotalShelves = rackShelves.Count,
                            UsedShelves = rackShelves.Count(s => s.CurrentQty > 0),
                            UtilizationPercentage = rkUtil
                        };
                    })
                    .OrderBy(r => r.RackCode)
                    .ToList();

                // Available Racks in Selected Room
                AvailableRacks = RoomId.HasValue
                    ? allWarehouseRacks.Where(rk => rk.RoomId == RoomId.Value).ToList()
                    : allWarehouseRacks;

                // Select active Rack: Prioritize rack with shelves/stock
                if (!RackId.HasValue || !AvailableRacks.Any(r => r.RackId == RackId.Value))
                {
                    RackId = AvailableRacks.FirstOrDefault(rk => rk.TotalShelves > 0)?.RackId ?? AvailableRacks.FirstOrDefault()?.RackId;
                }

                // 7. Build Physical Rack Elevation ViewModel (Center Panel in Stitch)
                var currentRackShelves = RackId.HasValue
                    ? warehouseData.Where(s => s.RackID == RackId.Value && s.ShelfID > 0).ToList()
                    : actualShelves.Take(4).ToList();

                if (RackId.HasValue && warehouseData.Any(s => s.RackID == RackId.Value))
                {
                    var rkFirst = warehouseData.First(s => s.RackID == RackId.Value);
                    ActiveRackElevation = new RackElevationViewModel
                    {
                        RackId = rkFirst.RackID,
                        RackCode = rkFirst.RackCode,
                        RackName = string.IsNullOrWhiteSpace(rkFirst.RackName) ? rkFirst.RackCode : rkFirst.RackName,
                        RoomId = rkFirst.RoomID,
                        RoomCode = rkFirst.RoomCode,
                        RoomName = rkFirst.RoomName,
                        FloorCode = rkFirst.FloorCode,
                        FloorName = rkFirst.FloorName,
                        WarehouseCode = rkFirst.WarehouseCode,
                        WarehouseName = rkFirst.WarehouseName
                    };

                    // Group shelves by tier level
                    var shelfItems = currentRackShelves.Select((s, index) =>
                    {
                        int tier = ExtractTierLevel(s.ShelfCode, s.ShelfName, index + 1);
                        int util = s.MaxQty > 0 ? (int)Math.Round((s.CurrentQty / s.MaxQty) * 100) : 0;
                        var statusKey = GetShelfStatusKey(util, s.CurrentQty, s.ShelfType, s.IsActive);
                        var statusLabel = GetShelfStatusLabel(util, s.CurrentQty, s.ShelfType, s.IsActive);

                        return new ShelfSlotViewModel
                        {
                            ShelfId = s.ShelfID,
                            ShelfCode = s.ShelfCode,
                            ShelfName = s.ShelfName,
                            ShelfType = string.IsNullOrWhiteSpace(s.ShelfType) ? "General" : s.ShelfType,
                            TierLevel = tier,
                            CurrentQty = s.CurrentQty,
                            MaxQty = s.MaxQty,
                            AvailableQty = s.AvailableQty,
                            AvailableCapacity = Math.Max(0, s.MaxQty - s.CurrentQty),
                            CurrentWeightKg = s.CurrentWeightKg,
                            MaxWeightKg = s.MaxWeightKg,
                            UtilizationPercentage = util,
                            StatusKey = statusKey,
                            StatusLabel = statusLabel,
                            SKU = s.SKU,
                            ProductName = string.IsNullOrWhiteSpace(s.ProductName) ? "Empty Shelf" : s.ProductName,
                            ProductType = s.ProductType,
                            ReorderLevel = s.ReorderLevel,
                            IsActive = s.IsActive
                        };
                    }).ToList();

                    // Organize into distinct levels from top to bottom (Level 4 down to Level 1)
                    int maxTier = shelfItems.Any() ? Math.Max(4, shelfItems.Max(s => s.TierLevel)) : 4;
                    for (int t = maxTier; t >= 1; t--)
                    {
                        var slotsInTier = shelfItems.Where(s => s.TierLevel == t).OrderBy(s => s.ShelfCode).ToList();
                        ActiveRackElevation.Tiers.Add(new RackTierViewModel
                        {
                            LevelNumber = t,
                            LevelName = $"Level {t}",
                            Slots = slotsInTier
                        });
                    }
                }

                // 8. Select active Shelf for Details Panel (Right Panel in Stitch)
                var allWarehouseShelvesFlattened = actualShelves.Select(s =>
                {
                    int util = s.MaxQty > 0 ? (int)Math.Round((s.CurrentQty / s.MaxQty) * 100) : 0;
                    return new ShelfDetails
                    {
                        ShelfId = s.ShelfID,
                        Code = s.ShelfCode,
                        Name = s.ShelfName,
                        ShelfType = string.IsNullOrWhiteSpace(s.ShelfType) ? "General" : s.ShelfType,
                        CurrentQty = s.CurrentQty,
                        MaxQty = s.MaxQty,
                        AvailableQty = s.AvailableQty,
                        AvailableCapacity = Math.Max(0, s.MaxQty - s.CurrentQty),
                        CurrentWeight = s.CurrentWeightKg,
                        MaxWeight = s.MaxWeightKg,
                        UtilizationPercentage = util,
                        Status = GetShelfStatusLabel(util, s.CurrentQty, s.ShelfType, s.IsActive),
                        StatusKey = GetShelfStatusKey(util, s.CurrentQty, s.ShelfType, s.IsActive),
                        ProductName = string.IsNullOrWhiteSpace(s.ProductName) ? "Empty Shelf" : s.ProductName,
                        SKU = s.SKU,
                        ProductType = s.ProductType,
                        Warehouse = s.WarehouseName,
                        WarehouseCode = s.WarehouseCode,
                        Floor = string.IsNullOrWhiteSpace(s.FloorName) ? s.FloorCode : s.FloorName,
                        FloorCode = s.FloorCode,
                        Room = string.IsNullOrWhiteSpace(s.RoomName) ? s.RoomCode : s.RoomName,
                        RoomCode = s.RoomCode,
                        Rack = string.IsNullOrWhiteSpace(s.RackName) ? s.RackCode : s.RackName,
                        RackCode = s.RackCode,
                        Path = $"{s.WarehouseCode} > {s.FloorCode} > {s.RoomCode} > {s.RackCode} > {s.ShelfCode}"
                    };
                }).ToList();

                if (ShelfId.HasValue)
                {
                    SelectedShelf = allWarehouseShelvesFlattened.FirstOrDefault(s => s.ShelfId == ShelfId.Value)
                                    ?? allWarehouseShelvesFlattened.FirstOrDefault()
                                    ?? new ShelfDetails();
                }
                else
                {
                    // Default to first active shelf in the current rack, or first shelf in rack
                    var defaultInRack = ActiveRackElevation.Tiers
                        .SelectMany(t => t.Slots)
                        .FirstOrDefault(s => s.CurrentQty > 0);

                    int targetId = defaultInRack?.ShelfId
                                   ?? ActiveRackElevation.Tiers.SelectMany(t => t.Slots).FirstOrDefault()?.ShelfId
                                   ?? 0;

                    SelectedShelf = allWarehouseShelvesFlattened.FirstOrDefault(s => s.ShelfId == targetId)
                                    ?? allWarehouseShelvesFlattened.FirstOrDefault()
                                    ?? new ShelfDetails();
                }

                // 9. Lower Data Grid (Bottom Row in Stitch)
                // 9a. Room Utilization Breakdown
                RoomUtilizationSummary = Rooms.Select(r => new RoomUtilizationSummaryItem
                {
                    RoomCode = r.RoomCode,
                    RoomName = r.RoomName,
                    UtilizationPercentage = r.UtilizationPercentage,
                    TotalShelves = r.TotalShelves,
                    UsedShelves = r.UsedShelves,
                    StatusColor = r.StatusColor
                }).ToList();

                // 9b. Rack Utilization Table
                RackUtilizationTable = actualShelves
                    .Where(s => s.RackID > 0)
                    .GroupBy(s => s.RackID)
                    .Select(g =>
                    {
                        var rk = g.First();
                        decimal rkMax = g.Sum(s => s.MaxQty);
                        decimal rkUsed = g.Sum(s => s.CurrentQty);
                        int rkUtil = rkMax > 0 ? (int)Math.Round((rkUsed / rkMax) * 100) : 0;
                        int total = g.Count();
                        int used = g.Count(s => s.CurrentQty > 0);
                        int avail = Math.Max(0, total - used);

                        return new RackUtilizationTableItem
                        {
                            RackId = rk.RackID,
                            FloorCode = rk.FloorCode,
                            RoomCode = rk.RoomCode,
                            RoomName = rk.RoomName,
                            RackCode = rk.RackCode,
                            RackName = rk.RackName,
                            TotalShelves = total,
                            UsedShelves = used,
                            AvailableShelves = avail,
                            UtilizationPercentage = rkUtil,
                            StatusColor = GetStatusColor(rkUtil)
                        };
                    })
                    .OrderBy(r => r.FloorCode)
                    .ThenBy(r => r.RoomCode)
                    .ThenBy(r => r.RackCode)
                    .ToList();

                // 9c. Low Stock Shelves Alert List
                LowStockShelves = actualShelves
                    .Where(s => s.CurrentQty > 0 && s.AvailableQty <= s.ReorderLevel && s.AvailableQty > 0)
                    .Select(s => new LowStockShelfItem
                    {
                        ShelfCode = s.ShelfCode,
                        ProductName = s.ProductName,
                        SKU = s.SKU,
                        Quantity = (int)s.AvailableQty,
                        Unit = "pcs",
                        ReorderLevel = (int)s.ReorderLevel
                    })
                    .Take(6)
                    .ToList();

                // 10. Client JSON Payload with ALL warehouse racks and valid shelves
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };

                var clientPayload = new
                {
                    warehouseId = WarehouseId,
                    floorId = FloorId,
                    roomId = RoomId,
                    rackId = RackId,
                    shelfId = SelectedShelf.ShelfId,
                    shelves = allWarehouseShelvesFlattened,
                    rooms = Rooms,
                    racks = allWarehouseRacks, // Pass ALL warehouse racks for client-side room switching
                    activeRack = ActiveRackElevation
                };

                VisualizationDataJson = JsonSerializer.Serialize(clientPayload, jsonOptions);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Database error: {ex.Message}";
            }
        }

        private static int ExtractTierLevel(string shelfCode, string shelfName, int fallbackIndex)
        {
            if (!string.IsNullOrWhiteSpace(shelfCode))
            {
                var parts = shelfCode.Split('-');
                if (parts.Length > 0)
                {
                    var last = parts[^1].Trim();
                    if (last.StartsWith("S", StringComparison.OrdinalIgnoreCase) && int.TryParse(last.Substring(1), out int parsedTier))
                    {
                        return Math.Clamp(parsedTier, 1, 6);
                    }
                    if (last.StartsWith("L", StringComparison.OrdinalIgnoreCase) && int.TryParse(last.Substring(1), out int parsedLevel))
                    {
                        return Math.Clamp(parsedLevel, 1, 6);
                    }
                    var digits = new string(last.Where(char.IsDigit).ToArray());
                    if (int.TryParse(digits, out int extractedNum) && extractedNum >= 1 && extractedNum <= 6)
                    {
                        return extractedNum;
                    }
                    if (int.TryParse(last, out int numTier))
                    {
                        return Math.Clamp(numTier, 1, 6);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(shelfName))
            {
                if (shelfName.Contains("Tier 4", StringComparison.OrdinalIgnoreCase) || shelfName.Contains("Level 4", StringComparison.OrdinalIgnoreCase)) return 4;
                if (shelfName.Contains("Tier 3", StringComparison.OrdinalIgnoreCase) || shelfName.Contains("Level 3", StringComparison.OrdinalIgnoreCase)) return 3;
                if (shelfName.Contains("Tier 2", StringComparison.OrdinalIgnoreCase) || shelfName.Contains("Level 2", StringComparison.OrdinalIgnoreCase)) return 2;
                if (shelfName.Contains("Tier 1", StringComparison.OrdinalIgnoreCase) || shelfName.Contains("Level 1", StringComparison.OrdinalIgnoreCase)) return 1;
            }

            // Fallback cycling 1..4 based on index
            return ((fallbackIndex - 1) % 4) + 1;
        }

        private static string GetStatusColor(int utilization)
        {
            if (utilization >= 90) return "danger";
            if (utilization >= 70) return "warning";
            if (utilization > 0) return "success";
            return "muted";
        }

        private static string GetShelfStatusKey(int utilization, decimal currentQty, string shelfType, bool isActive)
        {
            if (!isActive) return "status-inactive";
            if (string.Equals(shelfType, "QC Hold", StringComparison.OrdinalIgnoreCase)) return "status-qchold";
            if (currentQty <= 0) return "status-empty";
            if (utilization >= 90) return "status-full";
            if (utilization >= 70) return "status-high";
            if (utilization >= 30) return "status-medium";
            return "status-low";
        }

        private static string GetShelfStatusLabel(int utilization, decimal currentQty, string shelfType, bool isActive)
        {
            if (!isActive) return "Inactive";
            if (string.Equals(shelfType, "QC Hold", StringComparison.OrdinalIgnoreCase)) return "QC Hold";
            if (currentQty <= 0) return "Empty";
            if (utilization >= 90) return "Full";
            if (utilization >= 70) return "High (70-90%)";
            if (utilization >= 30) return "Medium (30-70%)";
            return "Low (0-30%)";
        }
    }

    public class WarehouseDropdownOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public class FloorDropdownOption
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int ShelfCount { get; set; }
    }

    public class WarehouseHeaderInfo
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal TotalCapacity { get; set; }
        public decimal UsedCapacity { get; set; }
        public int UtilizationPercentage { get; set; }
        public int TotalFloors { get; set; }
        public int TotalRooms { get; set; }
        public int TotalRacks { get; set; }
        public int TotalShelves { get; set; }
        public int UsedShelves { get; set; }
    }

    public class VisualizationSummaryCard
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Subtext { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
        public bool IsCapacityCard { get; set; }
        public int Percentage { get; set; }
    }

    public class RoomLayoutItem
    {
        public int RoomId { get; set; }
        public string RoomCode { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public int FloorId { get; set; }
        public string FloorCode { get; set; } = string.Empty;
        public string FloorName { get; set; } = string.Empty;
        public int TotalShelves { get; set; }
        public int UsedShelves { get; set; }
        public int UtilizationPercentage { get; set; }
        public string StatusColor { get; set; } = string.Empty;
        public bool HasRawMaterial { get; set; }
        public bool HasFinishedGoods { get; set; }
        public bool HasQCHold { get; set; }
    }

    public class RackOptionItem
    {
        public int RackId { get; set; }
        public string RackCode { get; set; } = string.Empty;
        public string RackName { get; set; } = string.Empty;
        public int RoomId { get; set; }
        public string RoomCode { get; set; } = string.Empty;
        public int TotalShelves { get; set; }
        public int UsedShelves { get; set; }
        public int UtilizationPercentage { get; set; }
    }

    public class RackElevationViewModel
    {
        public int RackId { get; set; }
        public string RackCode { get; set; } = string.Empty;
        public string RackName { get; set; } = string.Empty;
        public int RoomId { get; set; }
        public string RoomCode { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string FloorCode { get; set; } = string.Empty;
        public string FloorName { get; set; } = string.Empty;
        public string WarehouseCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public List<RackTierViewModel> Tiers { get; set; } = new();
    }

    public class RackTierViewModel
    {
        public int LevelNumber { get; set; }
        public string LevelName { get; set; } = string.Empty;
        public List<ShelfSlotViewModel> Slots { get; set; } = new();
    }

    public class ShelfSlotViewModel
    {
        public int ShelfId { get; set; }
        public string ShelfCode { get; set; } = string.Empty;
        public string ShelfName { get; set; } = string.Empty;
        public string ShelfType { get; set; } = "General";
        public int TierLevel { get; set; }
        public decimal CurrentQty { get; set; }
        public decimal MaxQty { get; set; }
        public decimal AvailableQty { get; set; }
        public decimal AvailableCapacity { get; set; }
        public decimal CurrentWeightKg { get; set; }
        public decimal MaxWeightKg { get; set; }
        public int UtilizationPercentage { get; set; }
        public string StatusKey { get; set; } = "empty";
        public string StatusLabel { get; set; } = "Empty";
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public decimal ReorderLevel { get; set; }
        public bool IsActive { get; set; }
    }

    public class ShelfDetails
    {
        public int ShelfId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string ShelfType { get; set; } = string.Empty;
        public decimal CurrentQty { get; set; }
        public decimal MaxQty { get; set; }
        public decimal AvailableQty { get; set; }
        public decimal AvailableCapacity { get; set; }
        public int UtilizationPercentage { get; set; }
        public decimal CurrentWeight { get; set; }
        public decimal MaxWeight { get; set; }
        public string Warehouse { get; set; } = string.Empty;
        public string WarehouseCode { get; set; } = string.Empty;
        public string Floor { get; set; } = string.Empty;
        public string FloorCode { get; set; } = string.Empty;
        public string Room { get; set; } = string.Empty;
        public string RoomCode { get; set; } = string.Empty;
        public string Rack { get; set; } = string.Empty;
        public string RackCode { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusKey { get; set; } = "empty";
    }

    public class RoomUtilizationSummaryItem
    {
        public string RoomCode { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public int UtilizationPercentage { get; set; }
        public int TotalShelves { get; set; }
        public int UsedShelves { get; set; }
        public string StatusColor { get; set; } = string.Empty;
    }

    public class RackUtilizationTableItem
    {
        public int RackId { get; set; }
        public string FloorCode { get; set; } = string.Empty;
        public string RoomCode { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string RackCode { get; set; } = string.Empty;
        public string RackName { get; set; } = string.Empty;
        public int TotalShelves { get; set; }
        public int UsedShelves { get; set; }
        public int AvailableShelves { get; set; }
        public int UtilizationPercentage { get; set; }
        public string StatusColor { get; set; } = string.Empty;
    }

    public class LowStockShelfItem
    {
        public string ShelfCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Unit { get; set; } = "pcs";
        public int ReorderLevel { get; set; }
    }
}

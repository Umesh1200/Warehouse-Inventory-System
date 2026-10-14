using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using IMS.Services;
using IMS.Repositories;
using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace IMS.Pages.Warehouses
{
    public class SetupModel : PageModel
    {
        private readonly IWarehouseService _warehouseService;
        private readonly IWarehouseRepository _warehouseRepository;
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public SetupModel(IWarehouseService warehouseService, IWarehouseRepository warehouseRepository)
        {
            _warehouseService = warehouseService;
            _warehouseRepository = warehouseRepository;
        }

        public List<SetupSummaryCard> SummaryCards { get; set; } = new();
        public List<ShelfSetupRow> Shelves { get; set; } = new();
        public List<string> Warehouses { get; set; } = new();
        public List<SetupOption> WarehouseOptions { get; set; } = new();
        public List<SetupOption> FloorOptions { get; set; } = new();
        public List<SetupOption> RoomOptions { get; set; } = new();
        public List<SetupOption> RackOptions { get; set; } = new();
        public WarehouseSummaryDto Summary { get; set; } = new();

        public async Task<IActionResult> OnPostCreateWarehouseAsync(string code, string name, string location)
        {
            await _warehouseService.CreateWarehouseAsync(code, name, location ?? string.Empty);
            TempData["SuccessMessage"] = "Warehouse created successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateWarehouseAsync(int id, string code, string name, string location)
        {
            await _warehouseService.UpdateWarehouseAsync(id, code, name, location);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleWarehouseStatusAsync(int id, bool isActive)
        {
            await _warehouseService.ToggleWarehouseStatusAsync(id, isActive);
            return RedirectToPage();
        }

        // Floor Handlers
        public async Task<IActionResult> OnPostCreateFloorAsync(int warehouseId, string code, string name)
        {
            await _warehouseService.CreateFloorAsync(warehouseId, code, name);
            TempData["SuccessMessage"] = "Floor created successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateFloorAsync(int id, int warehouseId, string code, string name)
        {
            await _warehouseService.UpdateFloorAsync(id, warehouseId, code, name);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleFloorStatusAsync(int id, bool isActive)
        {
            try { await _warehouseService.ToggleFloorStatusAsync(id, isActive); }
            catch (Exception ex) { ErrorMessage = ex.Message; await LoadDataAsync(); return Page(); }
            return RedirectToPage();
        }

        // Room Handlers
        public async Task<IActionResult> OnPostCreateRoomAsync(int floorId, string code, string name)
        {
            await _warehouseService.CreateRoomAsync(floorId, code, name);
            TempData["SuccessMessage"] = "Room created successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateRoomAsync(int id, int floorId, string code, string name)
        {
            await _warehouseService.UpdateRoomAsync(id, floorId, code, name);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleRoomStatusAsync(int id, bool isActive)
        {
            try { await _warehouseService.ToggleRoomStatusAsync(id, isActive); }
            catch (Exception ex) { ErrorMessage = ex.Message; await LoadDataAsync(); return Page(); }
            return RedirectToPage();
        }

        // Rack Handlers
        public async Task<IActionResult> OnPostCreateRackAsync(int roomId, string code, string name)
        {
            await _warehouseService.CreateRackAsync(roomId, code, name);
            TempData["SuccessMessage"] = "Rack created successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateRackAsync(int id, int roomId, string code, string name)
        {
            await _warehouseService.UpdateRackAsync(id, roomId, code, name);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleRackStatusAsync(int id, bool isActive)
        {
            try { await _warehouseService.ToggleRackStatusAsync(id, isActive); }
            catch (Exception ex) { ErrorMessage = ex.Message; await LoadDataAsync(); return Page(); }
            return RedirectToPage();
        }

        // Shelf Handlers
        public async Task<IActionResult> OnPostCreateShelfAsync(int rackId, string code, string name, string type, decimal maxQty, decimal maxWeight, int tierLevel = 1)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                name = $"Level {tierLevel} Shelf";
            }
            else if (!name.Contains("Tier", StringComparison.OrdinalIgnoreCase) && !name.Contains("Level", StringComparison.OrdinalIgnoreCase))
            {
                name = $"{name} (Level {tierLevel})";
            }
            await _warehouseService.CreateShelfAsync(rackId, code, name, type, maxQty, maxWeight);
            TempData["SuccessMessage"] = $"Shelf on Level {tierLevel} created successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateShelfAsync(int id, int rackId, string code, string name, string type, decimal maxQty, decimal maxWeight, int tierLevel = 1)
        {
            try 
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = $"Level {tierLevel} Shelf";
                }
                else if (!name.Contains("Tier", StringComparison.OrdinalIgnoreCase) && !name.Contains("Level", StringComparison.OrdinalIgnoreCase))
                {
                    name = $"{name} (Level {tierLevel})";
                }
                await _warehouseService.UpdateShelfAsync(id, rackId, code, name, type, maxQty, maxWeight); 
                TempData["SuccessMessage"] = "Shelf updated successfully.";
            }
            catch (Exception ex) { ErrorMessage = ex.Message; await LoadDataAsync(); return Page(); }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleShelfStatusAsync(int id, bool isActive)
        {
            try { await _warehouseService.ToggleShelfStatusAsync(id, isActive); }
            catch (Exception ex) { ErrorMessage = ex.Message; await LoadDataAsync(); return Page(); }
            return RedirectToPage();
        }

        // Edit GET Handlers
        public async Task<IActionResult> OnGetWarehouseAsync(int id) => new JsonResult(await _warehouseRepository.GetWarehouseByIdAsync(id));
        public async Task<IActionResult> OnGetFloorAsync(int id) => new JsonResult(await _warehouseRepository.GetFloorByIdAsync(id));
        public async Task<IActionResult> OnGetRoomAsync(int id) => new JsonResult(await _warehouseRepository.GetRoomByIdAsync(id));
        public async Task<IActionResult> OnGetRackAsync(int id) => new JsonResult(await _warehouseRepository.GetRackByIdAsync(id));
        public async Task<IActionResult> OnGetShelfAsync(int id) => new JsonResult(await _warehouseRepository.GetShelfByIdAsync(id));

        private async Task LoadDataAsync()
        {
            try
            {
                SuccessMessage = TempData["SuccessMessage"]?.ToString() ?? string.Empty;
                var shelvesData = await _warehouseService.GetWarehouseSetupAsync();
                Summary = await _warehouseService.GetWarehouseSummaryAsync();

                Warehouses = new List<string> { "All Warehouses" };
                Warehouses.AddRange(shelvesData.Select(s => s.WarehouseName).Distinct());
                WarehouseOptions = shelvesData
                    .Where(s => s.WarehouseID != 0)
                    .GroupBy(s => s.WarehouseID)
                    .Select(g => new SetupOption { ID = g.Key, Name = $"{g.First().WarehouseCode} - {g.First().WarehouseName}" })
                    .OrderBy(o => o.Name)
                    .ToList();
                FloorOptions = shelvesData
                    .Where(s => s.FloorID != 0)
                    .GroupBy(s => s.FloorID)
                    .Select(g => new SetupOption { ID = g.Key, Name = $"{g.First().WarehouseCode} / {g.First().FloorCode} - {g.First().FloorName}" })
                    .OrderBy(o => o.Name)
                    .ToList();
                RoomOptions = shelvesData
                    .Where(s => s.RoomID != 0)
                    .GroupBy(s => s.RoomID)
                    .Select(g => new SetupOption { ID = g.Key, Name = $"{g.First().FloorCode} / {g.First().RoomCode} - {g.First().RoomName}" })
                    .OrderBy(o => o.Name)
                    .ToList();
                RackOptions = shelvesData
                    .Where(s => s.RackID != 0)
                    .GroupBy(s => s.RackID)
                    .Select(g => new SetupOption { ID = g.Key, Name = $"{g.First().RoomCode} / {g.First().RackCode} - {g.First().RackName}" })
                    .OrderBy(o => o.Name)
                    .ToList();

                SummaryCards = new List<SetupSummaryCard>
                {
                    new SetupSummaryCard { Title = "Total Warehouses", Value = Summary.TotalWarehouses.ToString(), Icon = "home_work", ColorClass = "primary" },
                    new SetupSummaryCard { Title = "Active Shelves", Value = Summary.ActiveShelves.ToString("N0"), Icon = "shelves", ColorClass = "success" },
                    new SetupSummaryCard { Title = "QC Hold Shelves", Value = Summary.QCHoldShelves.ToString(), Icon = "fact_check", ColorClass = "warning" },
                    new SetupSummaryCard { Title = "Capacity Warnings", Value = Summary.CapacityWarnings.ToString(), Icon = "report_problem", ColorClass = "danger" }
                };

                Shelves = shelvesData.Select(s => new ShelfSetupRow
                {
                    ShelfID = s.ShelfID,
                    WarehouseID = s.WarehouseID,
                    FloorID = s.FloorID,
                    RoomID = s.RoomID,
                    RackID = s.RackID,
                    WarehouseActive = s.WarehouseActive,
                    FloorActive = s.FloorActive,
                    RoomActive = s.RoomActive,
                    RackActive = s.RackActive,
                    Warehouse = s.WarehouseName,
                    WarehouseCode = s.WarehouseCode,
                    Floor = s.FloorCode,
                    Room = s.RoomCode,
                    Rack = s.RackCode,
                    Code = s.ShelfCode,
                    Name = s.ShelfName,
                    Type = s.ShelfType,
                    TierLevel = ExtractTier(s.ShelfCode, s.ShelfName),
                    MaxQty = (int)s.MaxQty,
                    MaxWeight = s.MaxWeightKg,
                    CurrentQty = s.CurrentQty,
                    Utilization = (int)s.UtilizationPercentage,
                    Status = s.IsActive ? "Active" : "Inactive",
                    IsWarning = s.UtilizationPercentage >= 80,
                    IsQCHold = s.ShelfType == "QC Hold"
                }).ToList();
            }
            catch (Exception)
            {
                ErrorMessage = "Database connection failed. Showing offline mode.";
                Shelves = new List<ShelfSetupRow>();
                Summary = new WarehouseSummaryDto();
            }
        }

        private static int ExtractTier(string code, string name)
        {
            if (!string.IsNullOrWhiteSpace(code))
            {
                var parts = code.Split('-');
                if (parts.Length > 0)
                {
                    var last = parts[^1].Trim();
                    if (last.StartsWith("S", StringComparison.OrdinalIgnoreCase) && int.TryParse(last.Substring(1), out int parsedTier))
                        return Math.Clamp(parsedTier, 1, 6);
                    if (last.StartsWith("L", StringComparison.OrdinalIgnoreCase) && int.TryParse(last.Substring(1), out int parsedLevel))
                        return Math.Clamp(parsedLevel, 1, 6);
                    var digits = new string(last.Where(char.IsDigit).ToArray());
                    if (int.TryParse(digits, out int num) && num >= 1 && num <= 6)
                        return num;
                }
            }
            if (!string.IsNullOrWhiteSpace(name))
            {
                if (name.Contains("Tier 4", StringComparison.OrdinalIgnoreCase) || name.Contains("Level 4", StringComparison.OrdinalIgnoreCase)) return 4;
                if (name.Contains("Tier 3", StringComparison.OrdinalIgnoreCase) || name.Contains("Level 3", StringComparison.OrdinalIgnoreCase)) return 3;
                if (name.Contains("Tier 2", StringComparison.OrdinalIgnoreCase) || name.Contains("Level 2", StringComparison.OrdinalIgnoreCase)) return 2;
                if (name.Contains("Tier 1", StringComparison.OrdinalIgnoreCase) || name.Contains("Level 1", StringComparison.OrdinalIgnoreCase)) return 1;
            }
            return 1;
        }

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }
    }

    public class SetupSummaryCard
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
    }

    public class ShelfSetupRow
    {
        public int ShelfID { get; set; }
        public int WarehouseID { get; set; }
        public int FloorID { get; set; }
        public int RoomID { get; set; }
        public int RackID { get; set; }
        public bool WarehouseActive { get; set; }
        public bool FloorActive { get; set; }
        public bool RoomActive { get; set; }
        public bool RackActive { get; set; }
        public string Warehouse { get; set; } = string.Empty;
        public string WarehouseCode { get; set; } = string.Empty;
        public string Floor { get; set; } = string.Empty;
        public string Room { get; set; } = string.Empty;
        public string Rack { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Raw Material, Finished Goods, Consumable, QC Hold
        public int TierLevel { get; set; } = 1;
        public int MaxQty { get; set; }
        public decimal MaxWeight { get; set; }
        public decimal CurrentQty { get; set; }
        public int Utilization { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsWarning { get; set; }
        public bool IsQCHold { get; set; }
    }

    public class SetupOption
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}

using IMS.Models.DTOs;
using IMS.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly IWarehouseRepository _warehouseRepository;

        public WarehouseService(IWarehouseRepository warehouseRepository)
        {
            _warehouseRepository = warehouseRepository;
        }

        public async Task<IEnumerable<WarehouseSetupDto>> GetWarehouseSetupAsync()
        {
            return await _warehouseRepository.GetAllShelvesWithHierarchyAsync();
        }

        public async Task<IEnumerable<WarehouseSetupDto>> GetWarehouseVisualizationAsync()
        {
            return await _warehouseRepository.GetWarehouseVisualizationAsync();
        }

        public async Task<WarehouseSummaryDto> GetWarehouseSummaryAsync()
        {
            var shelves = await _warehouseRepository.GetWarehouseVisualizationAsync();
            var shelvesList = shelves.ToList();

            // Sum up quantities for true visualization totals
            // (Distinct by ShelfID for capacity/utilization)
            var distinctShelves = shelvesList.GroupBy(s => s.ShelfID).Select(g => g.First()).ToList();

            return new WarehouseSummaryDto
            {
                TotalWarehouses = distinctShelves.Select(s => s.WarehouseID).Distinct().Count(),
                ActiveShelves = distinctShelves.Count(s => s.IsActive),
                QCHoldShelves = distinctShelves.Count(s => s.ShelfType == "QC Hold"),
                CapacityWarnings = distinctShelves.Count(s => s.UtilizationPercentage >= 80),
                TotalCapacity = distinctShelves.Sum(s => s.MaxQty),
                TotalUsed = distinctShelves.Sum(s => s.CurrentQty)
            };
        }

        public async Task<IEnumerable<dynamic>> GetWarehousesAsync() => await _warehouseRepository.GetWarehousesAsync();
        public async Task<IEnumerable<dynamic>> GetFloorsByWarehouseAsync(int warehouseId) => await _warehouseRepository.GetFloorsByWarehouseAsync(warehouseId);
        public async Task<IEnumerable<dynamic>> GetRoomsByFloorAsync(int floorId) => await _warehouseRepository.GetRoomsByFloorAsync(floorId);
        public async Task<IEnumerable<dynamic>> GetRacksByRoomAsync(int roomId) => await _warehouseRepository.GetRacksByRoomAsync(roomId);
        public async Task<IEnumerable<dynamic>> GetShelvesByRackAsync(int rackId) => await _warehouseRepository.GetShelvesByRackAsync(rackId);
        public async Task<IEnumerable<dynamic>> GetShelvesByFloorOrWarehouseAsync(int? floorId, int? warehouseId) => await _warehouseRepository.GetShelvesByFloorOrWarehouseAsync(floorId, warehouseId);
        public async Task<int> CreateWarehouseAsync(string code, string name, string location) => await _warehouseRepository.CreateWarehouseAsync(code, name, location);
        public async Task<bool> UpdateWarehouseAsync(int id, string code, string name, string location) => await _warehouseRepository.UpdateWarehouseAsync(id, code, name, location);
        public async Task<bool> ToggleWarehouseStatusAsync(int id, bool isActive) => await _warehouseRepository.ToggleWarehouseStatusAsync(id, isActive);

        // Floor CRUD
        public async Task<int> CreateFloorAsync(int warehouseId, string code, string name) => await _warehouseRepository.CreateFloorAsync(warehouseId, code, name);
        public async Task<bool> UpdateFloorAsync(int id, int warehouseId, string code, string name) => await _warehouseRepository.UpdateFloorAsync(id, warehouseId, code, name);
        public async Task<bool> ToggleFloorStatusAsync(int id, bool isActive)
        {
            if (!isActive && await _warehouseRepository.GetFloorStockCountAsync(id) > 0) throw new System.Exception("Cannot deactivate floor with active stock.");
            return await _warehouseRepository.ToggleFloorStatusAsync(id, isActive);
        }

        // Room CRUD
        public async Task<int> CreateRoomAsync(int floorId, string code, string name) => await _warehouseRepository.CreateRoomAsync(floorId, code, name);
        public async Task<bool> UpdateRoomAsync(int id, int floorId, string code, string name) => await _warehouseRepository.UpdateRoomAsync(id, floorId, code, name);
        public async Task<bool> ToggleRoomStatusAsync(int id, bool isActive)
        {
            if (!isActive && await _warehouseRepository.GetRoomStockCountAsync(id) > 0) throw new System.Exception("Cannot deactivate room with active stock.");
            return await _warehouseRepository.ToggleRoomStatusAsync(id, isActive);
        }

        // Rack CRUD
        public async Task<int> CreateRackAsync(int roomId, string code, string name) => await _warehouseRepository.CreateRackAsync(roomId, code, name);
        public async Task<bool> UpdateRackAsync(int id, int roomId, string code, string name) => await _warehouseRepository.UpdateRackAsync(id, roomId, code, name);
        public async Task<bool> ToggleRackStatusAsync(int id, bool isActive)
        {
            if (!isActive && await _warehouseRepository.GetRackStockCountAsync(id) > 0) throw new System.Exception("Cannot deactivate rack with active stock.");
            return await _warehouseRepository.ToggleRackStatusAsync(id, isActive);
        }

        // Shelf CRUD
        public async Task<int> CreateShelfAsync(int rackId, string code, string name, string type, decimal maxQty, decimal maxWeight) => await _warehouseRepository.CreateShelfAsync(rackId, code, name, type, maxQty, maxWeight);
        public async Task<bool> UpdateShelfAsync(int id, int rackId, string code, string name, string type, decimal maxQty, decimal maxWeight)
        {
            var usage = await _warehouseRepository.GetShelfUsageAsync(id);
            if (maxQty < usage.UsedQty) throw new System.Exception("Cannot reduce MaxQty below current usage.");
            if (maxWeight < usage.UsedWeight) throw new System.Exception("Cannot reduce MaxWeight below current usage.");
            return await _warehouseRepository.UpdateShelfAsync(id, rackId, code, name, type, maxQty, maxWeight);
        }
        public async Task<bool> ToggleShelfStatusAsync(int id, bool isActive)
        {
            var usage = await _warehouseRepository.GetShelfUsageAsync(id);
            if (!isActive && usage.UsedQty > 0) throw new System.Exception("Cannot deactivate shelf with active stock.");
            return await _warehouseRepository.ToggleShelfStatusAsync(id, isActive);
        }

        // Product Locator
        public async Task<IEnumerable<ProductLocatorSearchResultDto>> SearchProductsForLocatorAsync(string query)
        {
            return await _warehouseRepository.SearchProductsForLocatorAsync(query);
        }

        public async Task<ProductLocatorDetailDto?> GetProductLocationDetailsAsync(int? productId, string? sku)
        {
            return await _warehouseRepository.GetProductLocationDetailsAsync(productId, sku);
        }
    }
}

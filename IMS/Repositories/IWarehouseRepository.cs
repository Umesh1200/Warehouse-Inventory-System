using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public interface IWarehouseRepository
    {
        Task<IEnumerable<WarehouseSetupDto>> GetAllShelvesWithHierarchyAsync();
        Task<IEnumerable<WarehouseSetupDto>> GetWarehouseVisualizationAsync();
        Task<IEnumerable<dynamic>> GetWarehousesAsync();
        Task<IEnumerable<dynamic>> GetFloorsByWarehouseAsync(int warehouseId);
        Task<IEnumerable<dynamic>> GetRoomsByFloorAsync(int floorId);
        Task<IEnumerable<dynamic>> GetRacksByRoomAsync(int roomId);
        Task<IEnumerable<dynamic>> GetShelvesByRackAsync(int rackId);
        Task<IEnumerable<dynamic>> GetShelvesByFloorOrWarehouseAsync(int? floorId, int? warehouseId);
        
        // Warehouse CRUD
        Task<int> CreateWarehouseAsync(string code, string name, string location);
        Task<bool> UpdateWarehouseAsync(int id, string code, string name, string location);
        Task<bool> ToggleWarehouseStatusAsync(int id, bool isActive);
        Task<dynamic?> GetWarehouseByIdAsync(int id);

        // Floor CRUD
        Task<int> CreateFloorAsync(int warehouseId, string code, string name);
        Task<bool> UpdateFloorAsync(int id, int warehouseId, string code, string name);
        Task<bool> ToggleFloorStatusAsync(int id, bool isActive);
        Task<bool> FloorCodeExistsAsync(int warehouseId, string code, int? excludeId = null);
        Task<int> GetFloorStockCountAsync(int floorId);
        Task<dynamic?> GetFloorByIdAsync(int id);

        // Room CRUD
        Task<int> CreateRoomAsync(int floorId, string code, string name);
        Task<bool> UpdateRoomAsync(int id, int floorId, string code, string name);
        Task<bool> ToggleRoomStatusAsync(int id, bool isActive);
        Task<bool> RoomCodeExistsAsync(int floorId, string code, int? excludeId = null);
        Task<int> GetRoomStockCountAsync(int roomId);
        Task<dynamic?> GetRoomByIdAsync(int id);

        // Rack CRUD
        Task<int> CreateRackAsync(int roomId, string code, string name);
        Task<bool> UpdateRackAsync(int id, int roomId, string code, string name);
        Task<bool> ToggleRackStatusAsync(int id, bool isActive);
        Task<bool> RackCodeExistsAsync(int roomId, string code, int? excludeId = null);
        Task<int> GetRackStockCountAsync(int rackId);
        Task<dynamic?> GetRackByIdAsync(int id);

        // Shelf CRUD
        Task<int> CreateShelfAsync(int rackId, string code, string name, string type, decimal maxQty, decimal maxWeight);
        Task<bool> UpdateShelfAsync(int id, int rackId, string code, string name, string type, decimal maxQty, decimal maxWeight);
        Task<bool> ToggleShelfStatusAsync(int id, bool isActive);
        Task<bool> ShelfCodeExistsAsync(int rackId, string code, int? excludeId = null);
        Task<(decimal UsedQty, decimal UsedWeight)> GetShelfUsageAsync(int shelfId);
        Task<dynamic?> GetShelfByIdAsync(int id);

        // Product Locator
        Task<IEnumerable<ProductLocatorSearchResultDto>> SearchProductsForLocatorAsync(string query);
        Task<ProductLocatorDetailDto?> GetProductLocationDetailsAsync(int? productId, string? sku);
    }
}

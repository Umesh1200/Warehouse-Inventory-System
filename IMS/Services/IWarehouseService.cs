using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Services
{
    public interface IWarehouseService
    {
        Task<IEnumerable<WarehouseSetupDto>> GetWarehouseSetupAsync();
        Task<IEnumerable<WarehouseSetupDto>> GetWarehouseVisualizationAsync();
        Task<WarehouseSummaryDto> GetWarehouseSummaryAsync();
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

        // Floor CRUD
        Task<int> CreateFloorAsync(int warehouseId, string code, string name);
        Task<bool> UpdateFloorAsync(int id, int warehouseId, string code, string name);
        Task<bool> ToggleFloorStatusAsync(int id, bool isActive);

        // Room CRUD
        Task<int> CreateRoomAsync(int floorId, string code, string name);
        Task<bool> UpdateRoomAsync(int id, int floorId, string code, string name);
        Task<bool> ToggleRoomStatusAsync(int id, bool isActive);

        // Rack CRUD
        Task<int> CreateRackAsync(int roomId, string code, string name);
        Task<bool> UpdateRackAsync(int id, int roomId, string code, string name);
        Task<bool> ToggleRackStatusAsync(int id, bool isActive);

        // Shelf CRUD
        Task<int> CreateShelfAsync(int rackId, string code, string name, string type, decimal maxQty, decimal maxWeight);
        Task<bool> UpdateShelfAsync(int id, int rackId, string code, string name, string type, decimal maxQty, decimal maxWeight);
        Task<bool> ToggleShelfStatusAsync(int id, bool isActive);

        // Product Locator
        Task<IEnumerable<ProductLocatorSearchResultDto>> SearchProductsForLocatorAsync(string query);
        Task<ProductLocatorDetailDto?> GetProductLocationDetailsAsync(int? productId, string? sku);
    }
}

using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public interface ISalesOrderRepository
    {
        Task<IEnumerable<SalesOrderDto>> GetAllSalesOrdersAsync();
        Task<IEnumerable<SalesOrderItemDto>> GetSalesOrderItemsAsync(int soid);
        Task<int> CreateSalesOrderWithItemsAsync(SalesOrderCreateDto so);
        Task<bool> UpdateSalesOrderWithItemsAsync(SalesOrderUpdateDto so);
        Task<bool> ConfirmSalesOrderAsync(int soId);
        Task<bool> CancelSalesOrderAsync(int soId);
        Task<SalesOrderDto?> GetSalesOrderByIdAsync(int soId);
        Task<bool> IsSONumberExistsAsync(string soNumber);
        Task<bool> IsSONumberExistsForOtherSOAsync(string soNumber, int soId);
        Task<IEnumerable<dynamic>> GetActiveCustomersAsync();
        Task<IEnumerable<dynamic>> GetActiveProductsAsync();
        Task<decimal> GetAvailableStockForProductAsync(int productId);
        Task<IEnumerable<InventoryOverviewDto>> GetProductStockLocationsAsync(int productId);
    }
}


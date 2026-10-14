using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Services
{
    public interface ISalesOrderService
    {
        Task<IEnumerable<SalesOrderDto>> GetSalesOrdersAsync();
        Task<IEnumerable<SalesOrderItemDto>> GetSalesOrderItemsAsync(int soid);
        Task<decimal> GetAvailableStockForProductAsync(int productId);
        Task<SalesOrderSummaryDto> GetSalesOrderSummaryAsync();
        Task<SalesOrderActionResultDto> CreateSalesOrderAsync(SalesOrderCreateDto so);
        Task<SalesOrderActionResultDto> UpdateSalesOrderAsync(SalesOrderUpdateDto so);
        Task<SalesOrderActionResultDto> ConfirmSalesOrderAsync(int soId);
        Task<SalesOrderActionResultDto> CancelSalesOrderAsync(int soId);
        Task<IEnumerable<dynamic>> GetActiveCustomersAsync();
        Task<IEnumerable<dynamic>> GetActiveProductsAsync();
        Task<IEnumerable<InventoryOverviewDto>> GetProductStockLocationsAsync(int productId);
    }
}

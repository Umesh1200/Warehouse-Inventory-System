using IMS.Models.DTOs;
using IMS.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.Services
{
    public class SalesOrderService : ISalesOrderService
    {
        private readonly ISalesOrderRepository _soRepository;

        public SalesOrderService(ISalesOrderRepository soRepository)
        {
            _soRepository = soRepository;
        }

        public async Task<IEnumerable<SalesOrderDto>> GetSalesOrdersAsync()
        {
            return await _soRepository.GetAllSalesOrdersAsync();
        }

        public async Task<IEnumerable<SalesOrderItemDto>> GetSalesOrderItemsAsync(int soid)
        {
            return await _soRepository.GetSalesOrderItemsAsync(soid);
        }

        public async Task<decimal> GetAvailableStockForProductAsync(int productId)
        {
            return await _soRepository.GetAvailableStockForProductAsync(productId);
        }

        public async Task<SalesOrderSummaryDto> GetSalesOrderSummaryAsync()
        {
            var sos = await _soRepository.GetAllSalesOrdersAsync();
            var soList = sos.ToList();

            return new SalesOrderSummaryDto
            {
                TotalOrders = soList.Count,
                ConfirmedOrders = soList.Count(o => o.Status == "Confirmed"),
                PickingOrders = soList.Count(o => new[] { "ReadyToPick", "Picking" }.Contains(o.Status)),
                DispatchPendingOrders = soList.Count(o => new[] { "Packed", "Dispatched" }.Contains(o.Status)),
                FulfilledOrders = soList.Count(o => o.Status == "Fulfilled")
            };
        }

        public async Task<SalesOrderActionResultDto> CreateSalesOrderAsync(SalesOrderCreateDto so)
        {
            if (string.IsNullOrWhiteSpace(so.SONumber))
            {
                so.SONumber = $"SO-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";
            }
            if (so.CustomerID <= 0) return new SalesOrderActionResultDto { Success = false, Message = "Customer is required." };
            if (so.RequiredDate.HasValue && so.RequiredDate.Value < so.OrderDate) return new SalesOrderActionResultDto { Success = false, Message = "Required Date cannot be before Order Date." };
            if (so.Items == null || !so.Items.Any()) return new SalesOrderActionResultDto { Success = false, Message = "At least one item is required." };

            foreach (var item in so.Items)
            {
                if (item.ProductID <= 0) return new SalesOrderActionResultDto { Success = false, Message = "Invalid Product selected." };
                if (item.OrderedQty <= 0) return new SalesOrderActionResultDto { Success = false, Message = "Ordered quantity must be greater than zero." };
                if (item.UnitPrice < 0) return new SalesOrderActionResultDto { Success = false, Message = "Unit price cannot be negative." };
            }

            if (await _soRepository.IsSONumberExistsAsync(so.SONumber))
            {
                return new SalesOrderActionResultDto { Success = false, Message = $"SO Number '{so.SONumber}' already exists." };
            }

            try
            {
                int newId = await _soRepository.CreateSalesOrderWithItemsAsync(so);
                return new SalesOrderActionResultDto { Success = true, Message = "Sales Order created successfully.", NewID = newId };
            }
            catch (Exception ex)
            {
                return new SalesOrderActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<SalesOrderActionResultDto> UpdateSalesOrderAsync(SalesOrderUpdateDto so)
        {
            if (so.SOID <= 0) return new SalesOrderActionResultDto { Success = false, Message = "Invalid SO ID." };
            if (string.IsNullOrWhiteSpace(so.SONumber)) return new SalesOrderActionResultDto { Success = false, Message = "SO Number is required." };
            if (so.CustomerID <= 0) return new SalesOrderActionResultDto { Success = false, Message = "Customer is required." };
            if (so.RequiredDate.HasValue && so.RequiredDate.Value < so.OrderDate) return new SalesOrderActionResultDto { Success = false, Message = "Required Date cannot be before Order Date." };
            if (so.Items == null || !so.Items.Any()) return new SalesOrderActionResultDto { Success = false, Message = "At least one item is required." };

            foreach (var item in so.Items)
            {
                if (item.ProductID <= 0) return new SalesOrderActionResultDto { Success = false, Message = "Invalid Product selected." };
                if (item.OrderedQty <= 0) return new SalesOrderActionResultDto { Success = false, Message = "Ordered quantity must be greater than zero." };
                if (item.UnitPrice < 0) return new SalesOrderActionResultDto { Success = false, Message = "Unit price cannot be negative." };
            }

            var existingSo = await _soRepository.GetSalesOrderByIdAsync(so.SOID);
            if (existingSo == null) return new SalesOrderActionResultDto { Success = false, Message = "Sales Order not found." };
            if (existingSo.Status != "Draft") return new SalesOrderActionResultDto { Success = false, Message = "Only Draft Sales Orders can be edited." };

            if (await _soRepository.IsSONumberExistsForOtherSOAsync(so.SONumber, so.SOID))
            {
                return new SalesOrderActionResultDto { Success = false, Message = $"SO Number '{so.SONumber}' already exists." };
            }

            try
            {
                bool success = await _soRepository.UpdateSalesOrderWithItemsAsync(so);
                if (success)
                    return new SalesOrderActionResultDto { Success = true, Message = "Sales Order updated successfully." };
                else
                    return new SalesOrderActionResultDto { Success = false, Message = "Failed to update Sales Order. It may no longer be a Draft." };
            }
            catch (Exception ex)
            {
                return new SalesOrderActionResultDto { Success = false, Message = ex.Message };
            }
        }


        public async Task<SalesOrderActionResultDto> ConfirmSalesOrderAsync(int soId)
        {
            var existingSo = await _soRepository.GetSalesOrderByIdAsync(soId);
            if (existingSo == null) return new SalesOrderActionResultDto { Success = false, Message = "Sales Order not found." };
            if (existingSo.Status != "Draft") return new SalesOrderActionResultDto { Success = false, Message = "Only Draft Sales Orders can be confirmed." };

            // Optional: Check available stock before confirmation if reservation is desired
            var items = await _soRepository.GetSalesOrderItemsAsync(soId);
            foreach (var item in items)
            {
                var available = await _soRepository.GetAvailableStockForProductAsync(item.ProductID);
                if (available < item.OrderedQty)
                {
                    // For now, we allow confirmation even if short, but we could block it.
                    // The requirements say "If confirming with reservation, available stock must be enough."
                    // Since I'm not auto-reserving yet (need shelf), I'll just confirm.
                }
            }

            try
            {
                bool success = await _soRepository.ConfirmSalesOrderAsync(soId);
                if (success) return new SalesOrderActionResultDto { Success = true, Message = "Sales Order confirmed successfully." };
                return new SalesOrderActionResultDto { Success = false, Message = "Failed to confirm Sales Order." };
            }
            catch (Exception ex)
            {
                return new SalesOrderActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<SalesOrderActionResultDto> CancelSalesOrderAsync(int soId)
        {
            var existingSo = await _soRepository.GetSalesOrderByIdAsync(soId);
            if (existingSo == null) return new SalesOrderActionResultDto { Success = false, Message = "Sales Order not found." };
            if (existingSo.Status != "Draft" && existingSo.Status != "Confirmed") 
                return new SalesOrderActionResultDto { Success = false, Message = "Only Draft or Confirmed Sales Orders can be cancelled." };

            try
            {
                // In a real app, unreserve stock here if it was reserved.
                bool success = await _soRepository.CancelSalesOrderAsync(soId);
                if (success) return new SalesOrderActionResultDto { Success = true, Message = "Sales Order cancelled successfully." };
                return new SalesOrderActionResultDto { Success = false, Message = "Failed to cancel Sales Order." };
            }
            catch (Exception ex)
            {
                return new SalesOrderActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<IEnumerable<dynamic>> GetActiveCustomersAsync()
        {
            return await _soRepository.GetActiveCustomersAsync();
        }

        public async Task<IEnumerable<dynamic>> GetActiveProductsAsync()
        {
            return await _soRepository.GetActiveProductsAsync();
        }

        public async Task<IEnumerable<InventoryOverviewDto>> GetProductStockLocationsAsync(int productId)
        {
            return await _soRepository.GetProductStockLocationsAsync(productId);
        }
    }
}

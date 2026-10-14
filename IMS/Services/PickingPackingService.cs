using IMS.Models.DTOs;
using IMS.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.Services
{
    public class PickingPackingService : IPickingPackingService
    {
        private readonly IPickingPackingRepository _repository;

        public PickingPackingService(IPickingPackingRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<PickingPackingDto>> GetPickPackTasksAsync()
        {
            return await _repository.GetPickPackTasksAsync();
        }

        public async Task<IEnumerable<PickingPackingItemDto>> GetTaskItemsAsync(int soid)
        {
            return await _repository.GetTaskItemsAsync(soid);
        }

        public async Task<PickingPackingSummaryDto> GetPickPackSummaryAsync()
        {
            var tasks = await _repository.GetPickPackTasksAsync();
            var taskList = tasks.ToList();

            return new PickingPackingSummaryDto
            {
                TotalPickTasks = taskList.Count,
                PendingPicking = taskList.Count(t => t.PickStatus == "Pending"),
                PickingInProgress = taskList.Count(t => t.PickStatus == "Picking"),
                PickedOrders = taskList.Count(t => t.PickStatus == "Picked"),
                PackedOrders = taskList.Count(t => t.PackStatus == "Packed")
            };
        }

        public async Task<PickingPackingActionResultDto> CreatePickTaskAsync(CreatePickTaskDto task)
        {
            if (task.SOID <= 0) return new PickingPackingActionResultDto { Success = false, Message = "Invalid Sales Order." };
            if (task.AssignedTo <= 0) return new PickingPackingActionResultDto { Success = false, Message = "Invalid Staff assignment." };
            
            var so = await _repository.GetSalesOrderForPickingAsync(task.SOID);
            if (so == null) return new PickingPackingActionResultDto { Success = false, Message = "Sales Order not found." };
            if (so.Status != "Confirmed" && so.Status != "Allocated") 
                return new PickingPackingActionResultDto { Success = false, Message = "Only Confirmed or Allocated Sales Orders can be allocated for picking." };

            if (task.Items == null || !task.Items.Any()) return new PickingPackingActionResultDto { Success = false, Message = "No items to reserve." };

            foreach (var item in task.Items)
            {
                var available = await _repository.GetAvailableStockAsync(item.ProductID, item.ShelfID);
                if (available < item.QtyToReserve)
                {
                    return new PickingPackingActionResultDto { Success = false, Message = $"Insufficient stock for Product ID {item.ProductID} on selected shelf. Available: {available}." };
                }
            }

            return await _repository.CreatePickTaskAsync(task);
        }

        public async Task<PickingPackingActionResultDto> StartPickingAsync(int pickId, int userId)
        {
            if (pickId <= 0) return new PickingPackingActionResultDto { Success = false, Message = "Invalid Pick ID." };
            return await _repository.StartPickingAsync(pickId, userId);
        }

        public async Task<PickingPackingActionResultDto> MarkPickedAsync(int pickId, int userId)
        {
            if (pickId <= 0) return new PickingPackingActionResultDto { Success = false, Message = "Invalid Pick ID." };
            return await _repository.MarkPickedAsync(pickId, userId);
        }

        public async Task<PickingPackingActionResultDto> CompletePackingAsync(PackingCreateDto packing)
        {
            if (packing.SOID <= 0) return new PickingPackingActionResultDto { Success = false, Message = "Invalid Sales Order." };
            if (packing.PackedBy <= 0) return new PickingPackingActionResultDto { Success = false, Message = "Invalid Staff assignment." };
            if (string.IsNullOrWhiteSpace(packing.ContainerType)) return new PickingPackingActionResultDto { Success = false, Message = "Container Type is required." };

            return await _repository.CompletePackingAsync(packing);
        }

        public async Task<SalesOrderForPickingDto?> GetSalesOrderForPickingAsync(int soId)
        {
            return await _repository.GetSalesOrderForPickingAsync(soId);
        }

        public async Task<IEnumerable<dynamic>> GetConfirmedSalesOrdersAsync()
        {
            return await _repository.GetConfirmedSalesOrdersAsync();
        }

        public async Task<IEnumerable<dynamic>> GetActiveWarehouseStaffAsync()
        {
            return await _repository.GetActiveWarehouseStaffAsync();
        }

        public async Task<IEnumerable<dynamic>> GetActiveShelvesAsync()
        {
            return await _repository.GetActiveShelvesAsync();
        }

        public async Task<IEnumerable<dynamic>> GetShelvesWithProductAsync(int productId)
        {
            return await _repository.GetShelvesWithProductAsync(productId);
        }
    }
}

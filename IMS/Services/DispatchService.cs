using IMS.Models.DTOs;
using IMS.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.Services
{
    public class DispatchService : IDispatchService
    {
        private readonly IDispatchRepository _repository;

        public DispatchService(IDispatchRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<DispatchTrackingDto>> GetDispatchTasksAsync()
        {
            return await _repository.GetDispatchTasksAsync();
        }

        public async Task<IEnumerable<DispatchItemDto>> GetDispatchItemsAsync(int soid)
        {
            return await _repository.GetDispatchItemsAsync(soid);
        }

        public async Task<DispatchSummaryDto> GetDispatchSummaryAsync()
        {
            var tasks = await _repository.GetDispatchTasksAsync();
            var taskList = tasks.ToList();

            return new DispatchSummaryDto
            {
                TotalDispatches = taskList.Count(t => t.DispatchID.HasValue),
                ReadyToDispatchCount = taskList.Count(t => t.Status == "Ready To Dispatch"),
                DispatchedCount = taskList.Count(t => t.Status == "Dispatched"),
                InTransitCount = taskList.Count(t => t.Status == "InTransit"),
                DeliveredCount = taskList.Count(t => t.Status == "Delivered"),
                FailedCount = taskList.Count(t => t.Status == "Failed")
            };
        }

        public async Task<DispatchActionResultDto> CreateDispatchAsync(DispatchCreateDto dispatch)
        {
            if (dispatch.SOID <= 0) return new DispatchActionResultDto { Success = false, Message = "Invalid Sales Order." };
            if (string.IsNullOrWhiteSpace(dispatch.CourierName)) return new DispatchActionResultDto { Success = false, Message = "Courier Name is required." };
            if (string.IsNullOrWhiteSpace(dispatch.VehicleNumber)) return new DispatchActionResultDto { Success = false, Message = "Vehicle Number is required." };
            if (string.IsNullOrWhiteSpace(dispatch.TrackingNumber)) return new DispatchActionResultDto { Success = false, Message = "Tracking Number is required." };

            if (await _repository.HasDispatchForSOAsync(dispatch.SOID))
            {
                return new DispatchActionResultDto { Success = false, Message = "A dispatch record already exists for this order." };
            }

            return await _repository.CreateDispatchAsync(dispatch);
        }

        public async Task<DispatchActionResultDto> ConfirmDispatchAsync(int dispatchId, int performedBy)
        {
            if (dispatchId <= 0) return new DispatchActionResultDto { Success = false, Message = "Invalid Dispatch ID." };
            
            var dispatch = await _repository.GetDispatchByIdAsync(dispatchId);
            if (dispatch == null) return new DispatchActionResultDto { Success = false, Message = "Dispatch record not found." };
            if (dispatch.Status != "Ready To Dispatch") return new DispatchActionResultDto { Success = false, Message = "Only dispatches in 'Ready To Dispatch' state can be confirmed." };

            return await _repository.ConfirmDispatchAsync(dispatchId, performedBy);
        }

        public async Task<DispatchActionResultDto> MarkDeliveredAsync(int dispatchId)
        {
            if (dispatchId <= 0) return new DispatchActionResultDto { Success = false, Message = "Invalid Dispatch ID." };
            return await _repository.MarkDeliveredAsync(dispatchId);
        }

        public async Task<DispatchActionResultDto> MarkFailedAsync(int dispatchId)
        {
            if (dispatchId <= 0) return new DispatchActionResultDto { Success = false, Message = "Invalid Dispatch ID." };
            return await _repository.MarkFailedAsync(dispatchId);
        }

        public async Task<SalesOrderForDispatchDto?> GetSalesOrderForDispatchAsync(int soId)
        {
            return await _repository.GetSalesOrderForDispatchAsync(soId);
        }
    }
}

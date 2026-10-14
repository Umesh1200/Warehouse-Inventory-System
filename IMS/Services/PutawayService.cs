using IMS.Models.DTOs;
using IMS.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.Services
{
    public class PutawayService : IPutawayService
    {
        private readonly IPutawayRepository _putawayRepository;

        public PutawayService(IPutawayRepository putawayRepository)
        {
            _putawayRepository = putawayRepository;
        }

        public async Task<IEnumerable<PutawayDto>> GetPutawayTasksAsync()
        {
            return await _putawayRepository.GetPutawayTasksAsync();
        }

        public async Task<PutawaySummaryDto> GetPutawaySummaryAsync()
        {
            var tasks = await _putawayRepository.GetPutawayTasksAsync();
            var taskList = tasks.ToList();

            return new PutawaySummaryDto
            {
                TotalTasks = taskList.Count(t => t.TaskID.HasValue),
                PendingPutaway = taskList.Count(t => t.Status == "Pending" || t.Status == "Pending Task"),
                InProgress = taskList.Count(t => t.Status == "In Progress"),
                Completed = taskList.Count(t => t.Status == "Completed"),
                QtyWaitingPutaway = taskList.Where(t => t.Status != "Completed").Sum(t => t.QtyToPutaway)
            };
        }

        public async Task<PutawayActionResultDto> CreatePutawayTaskAsync(PutawayTaskCreateDto task)
        {
            if (task.QCID <= 0) return new PutawayActionResultDto { Success = false, Message = "Invalid QC ID." };

            var qcItem = await _putawayRepository.GetQCItemForPutawayAsync(task.QCID);
            if (qcItem == null) return new PutawayActionResultDto { Success = false, Message = "QC record not found." };
            if (qcItem.AcceptedQty <= 0) return new PutawayActionResultDto { Success = false, Message = "No accepted quantity available for putaway." };

            if (await _putawayRepository.HasPutawayTaskForQCAsync(task.QCID))
            {
                return new PutawayActionResultDto { Success = false, Message = "A putaway task already exists for this QC record." };
            }

            task.ProductID = qcItem.ProductID;
            task.QtyToPutaway = qcItem.AcceptedQty;

            if (task.SuggestedShelfID.HasValue)
            {
                var compatible = await _putawayRepository.IsShelfCompatibleForProductAsync(task.ProductID, task.SuggestedShelfID.Value);
                if (!compatible)
                {
                    return new PutawayActionResultDto { Success = false, Message = "Suggested shelf is not compatible with this product storage rule." };
                }

                var quantityCapacity = await _putawayRepository.GetShelfAvailableCapacityAsync(task.SuggestedShelfID.Value);
                if (quantityCapacity < task.QtyToPutaway)
                {
                    return new PutawayActionResultDto { Success = false, Message = $"Suggested shelf does not have enough quantity capacity. Required: {task.QtyToPutaway}, Available: {quantityCapacity}." };
                }

                var requiredWeight = await _putawayRepository.GetRequiredWeightAsync(task.ProductID, task.QtyToPutaway);
                var weightCapacity = await _putawayRepository.GetShelfAvailableWeightCapacityAsync(task.SuggestedShelfID.Value);
                if (requiredWeight > weightCapacity)
                {
                    return new PutawayActionResultDto { Success = false, Message = $"Suggested shelf does not have enough weight capacity. Required: {requiredWeight:N2} kg, Available: {weightCapacity:N2} kg." };
                }
            }

            try
            {
                return await _putawayRepository.CreatePutawayTaskAsync(task);
            }
            catch (Exception ex)
            {
                return new PutawayActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<PutawayActionResultDto> CompletePutawayAsync(PutawayCompleteDto putaway)
        {
            if (putaway.TaskID <= 0) return new PutawayActionResultDto { Success = false, Message = "Invalid Task ID." };
            if (putaway.ActualShelfID <= 0) return new PutawayActionResultDto { Success = false, Message = "Invalid Actual Shelf selected." };

            var task = await _putawayRepository.GetPutawayTaskByIdAsync(putaway.TaskID);
            if (task == null) return new PutawayActionResultDto { Success = false, Message = "Putaway task not found." };
            if (task.Status == "Completed") return new PutawayActionResultDto { Success = false, Message = "Putaway task is already completed." };

            var compatible = await _putawayRepository.IsShelfCompatibleForProductAsync(task.ProductID, putaway.ActualShelfID);
            if (!compatible)
            {
                return new PutawayActionResultDto { Success = false, Message = "Actual shelf is not compatible with this product storage rule." };
            }

            var capacity = await _putawayRepository.GetShelfAvailableCapacityAsync(putaway.ActualShelfID);
            if (capacity < task.QtyToPutaway)
            {
                return new PutawayActionResultDto { Success = false, Message = $"Insufficient shelf capacity. Required: {task.QtyToPutaway}, Available: {capacity}." };
            }

            var requiredWeight = await _putawayRepository.GetRequiredWeightAsync(task.ProductID, task.QtyToPutaway);
            var weightCapacity = await _putawayRepository.GetShelfAvailableWeightCapacityAsync(putaway.ActualShelfID);
            if (requiredWeight > weightCapacity)
            {
                return new PutawayActionResultDto { Success = false, Message = $"Insufficient shelf weight capacity. Required: {requiredWeight:N2} kg, Available: {weightCapacity:N2} kg." };
            }

            try
            {
                return await _putawayRepository.CompletePutawayAsync(putaway);
            }
            catch (Exception ex)
            {
                return new PutawayActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<IEnumerable<ShelfOptionDto>> GetActiveShelvesAsync()
        {
            return await _putawayRepository.GetActiveShelvesAsync();
        }

        public async Task<IEnumerable<ShelfOptionDto>> GetSuggestedShelvesAsync(int productId, decimal quantity)
        {
            return await _putawayRepository.GetSuggestedShelvesAsync(productId, quantity);
        }
    }
}

using IMS.Models.DTOs;
using IMS.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.Services
{
    public class ReceivingService : IReceivingService
    {
        private readonly IReceivingRepository _receivingRepository;

        public ReceivingService(IReceivingRepository receivingRepository)
        {
            _receivingRepository = receivingRepository;
        }

        public async Task<IEnumerable<ReceivingDto>> GetReceivingTasksAsync()
        {
            return await _receivingRepository.GetReceivingTasksAsync();
        }

        public async Task<ReceivingSummaryDto> GetReceivingSummaryAsync()
        {
            var tasks = await _receivingRepository.GetReceivingTasksAsync();
            var taskList = tasks.ToList();

            return new ReceivingSummaryDto
            {
                TotalReceipts = taskList.Count(t => t.ReceiptID.HasValue),
                QCRequiredCount = taskList.Count(t => t.Status == "QC Required"),
                AcceptedReceipts = taskList.Count(t => t.Status == "Accepted"),
                TotalDamagedRejected = taskList.Sum(t => t.DamagedQty + t.RejectedQty),
                ExpectedTodayCount = taskList.Count(t => t.Status == "Expected")
            };
        }

        public async Task<PurchaseOrderItemReceivingDto?> GetPOItemForReceivingAsync(int poItemId)
        {
            return await _receivingRepository.GetPOItemForReceivingAsync(poItemId);
        }

        public async Task<ReceivingActionResultDto> ReceiveItemAsync(ReceivingCreateDto receipt)
        {
            if (receipt.POItemID <= 0) return new ReceivingActionResultDto { Success = false, Message = "Invalid PO Item." };
            if (receipt.ReceivedQty <= 0) return new ReceivingActionResultDto { Success = false, Message = "Received quantity must be greater than zero." };
            if (receipt.DamagedQty < 0) return new ReceivingActionResultDto { Success = false, Message = "Damaged quantity cannot be negative." };
            if (receipt.RejectedQty < 0) return new ReceivingActionResultDto { Success = false, Message = "Rejected quantity cannot be negative." };
            
            if ((receipt.DamagedQty + receipt.RejectedQty) > receipt.ReceivedQty)
            {
                return new ReceivingActionResultDto { Success = false, Message = "Damaged and Rejected quantities cannot exceed the total received quantity." };
            }

            var remainingQty = await _receivingRepository.GetRemainingQtyAsync(receipt.POItemID);
            if (receipt.ReceivedQty > remainingQty)
            {
                return new ReceivingActionResultDto { Success = false, Message = $"Cannot over-receive. Remaining expected quantity is {remainingQty}." };
            }

            try
            {
                int newId = await _receivingRepository.CreateReceiptAsync(receipt);
                return new ReceivingActionResultDto { Success = true, Message = "Receipt recorded successfully. Item moved to QC Required.", NewID = newId };
            }
            catch (Exception ex)
            {
                return new ReceivingActionResultDto { Success = false, Message = ex.Message };
            }
        }
    }
}

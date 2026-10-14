using IMS.Models.DTOs;
using IMS.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.Services
{
    public class FinishedGoodsReceiptService : IFinishedGoodsReceiptService
    {
        private readonly IFinishedGoodsReceiptRepository _repository;

        public FinishedGoodsReceiptService(IFinishedGoodsReceiptRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<FinishedGoodsReceiptDto>> GetReceiptsAsync()
        {
            return await _repository.GetAllReceiptsAsync();
        }

        public async Task<FinishedGoodsReceiptSummaryDto> GetReceiptSummaryAsync()
        {
            var receipts = await _repository.GetAllReceiptsAsync();
            var receiptList = receipts.ToList();

            return new FinishedGoodsReceiptSummaryDto
            {
                TotalReceipts = receiptList.Count,
                PendingQCCount = receiptList.Count(r => r.Status == "Pending QC"),
                TotalAcceptedQty = receiptList.Sum(r => r.AcceptedQty),
                TotalRejectedQty = receiptList.Sum(r => r.RejectedQty),
                CompletedReceipts = receiptList.Count(r => r.Status == "Completed" || r.Status == "Putaway Completed")
            };
        }

        public async Task<FinishedGoodsReceiptActionResultDto> CreateFinishedGoodsReceiptAsync(FinishedGoodsReceiptCreateDto receipt)
        {
            if (string.IsNullOrWhiteSpace(receipt.ReceiptNumber)) return new FinishedGoodsReceiptActionResultDto { Success = false, Message = "Receipt Number is required." };
            if (receipt.PlanID <= 0) return new FinishedGoodsReceiptActionResultDto { Success = false, Message = "Production Plan is required." };
            if (receipt.ProducedQty <= 0) return new FinishedGoodsReceiptActionResultDto { Success = false, Message = "Produced Quantity must be greater than zero." };
            if (receipt.AcceptedQty < 0) return new FinishedGoodsReceiptActionResultDto { Success = false, Message = "Accepted Quantity cannot be negative." };
            if (receipt.RejectedQty < 0) return new FinishedGoodsReceiptActionResultDto { Success = false, Message = "Rejected Quantity cannot be negative." };
            
            if ((receipt.AcceptedQty + receipt.RejectedQty) > receipt.ProducedQty)
            {
                return new FinishedGoodsReceiptActionResultDto { Success = false, Message = "Sum of Accepted and Rejected quantities cannot exceed Produced Quantity." };
            }

            var plan = await _repository.GetProductionPlanForReceiptAsync(receipt.PlanID);
            if (plan == null) return new FinishedGoodsReceiptActionResultDto { Success = false, Message = "Production Plan not found." };
            
            if (receipt.ProducedQty > plan.RemainingQty)
            {
                 // return new FinishedGoodsReceiptActionResultDto { Success = false, Message = $"Produced quantity ({receipt.ProducedQty}) exceeds remaining planned quantity ({plan.RemainingQty})." };
                 // Overproduction might be allowed in some businesses, but following requirements: "Do not allow receipt quantity to exceed remaining planned quantity unless overproduction is intentionally allowed."
                 // Let's block it for now.
                 return new FinishedGoodsReceiptActionResultDto { Success = false, Message = $"Produced quantity exceeds remaining planned quantity ({plan.RemainingQty:N0})." };
            }

            if (await _repository.IsReceiptNumberExistsAsync(receipt.ReceiptNumber))
            {
                return new FinishedGoodsReceiptActionResultDto { Success = false, Message = $"Receipt Number '{receipt.ReceiptNumber}' already exists." };
            }

            receipt.ProductID = plan.ProductID;

            try
            {
                int newId = await _repository.CreateFinishedGoodsReceiptAsync(receipt);
                // Call confirmation procedure to move it to QC Required
                await _repository.ReceiveFinishedGoodsAsync(newId, receipt.ReceivedBy);

                return new FinishedGoodsReceiptActionResultDto { Success = true, Message = "Finished Goods Receipt recorded and moved to QC Required.", NewID = newId };
            }
            catch (Exception ex)
            {
                return new FinishedGoodsReceiptActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<ProductionPlanForReceiptDto?> GetProductionPlanForReceiptAsync(int planId)
        {
            return await _repository.GetProductionPlanForReceiptAsync(planId);
        }

        public async Task<IEnumerable<dynamic>> GetActiveProductionPlansAsync()
        {
            return await _repository.GetActiveProductionPlansAsync();
        }

        public async Task<IEnumerable<dynamic>> GetActiveStaffAsync()
        {
            return await _repository.GetActiveStaffAsync();
        }
    }
}

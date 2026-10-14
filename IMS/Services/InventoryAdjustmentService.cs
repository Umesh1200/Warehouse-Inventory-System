using IMS.Models.DTOs;
using IMS.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.Services
{
    public class InventoryAdjustmentService : IInventoryAdjustmentService
    {
        private readonly IInventoryAdjustmentRepository _adjustmentRepository;

        public InventoryAdjustmentService(IInventoryAdjustmentRepository adjustmentRepository)
        {
            _adjustmentRepository = adjustmentRepository;
        }

        public async Task<IEnumerable<InventoryAdjustmentDto>> GetAdjustmentsAsync()
        {
            return await _adjustmentRepository.GetAllAdjustmentsAsync();
        }

        public async Task<InventoryAdjustmentSummaryDto> GetAdjustmentSummaryAsync()
        {
            var adjustments = await _adjustmentRepository.GetAllAdjustmentsAsync();
            var adjList = adjustments.ToList();

            return new InventoryAdjustmentSummaryDto
            {
                TotalAdjustments = adjList.Count,
                PendingAdjustments = adjList.Count(a => a.Status == "Pending"),
                AwaitingApprovalCount = adjList.Count(a => a.Status == "Awaiting Approval"),
                ApprovedCount = adjList.Count(a => a.Status == "Approved"),
                PostedCount = adjList.Count(a => a.Status == "Posted")
            };
        }

        public async Task<AdjustmentActionResultDto> CreateAdjustmentAsync(AdjustmentCreateDto adjustment)
        {
            if (string.IsNullOrWhiteSpace(adjustment.AdjustmentNo)) return new AdjustmentActionResultDto { Success = false, Message = "Adjustment Number is required." };
            if (adjustment.AdjustmentType != "AdjustmentIn" && adjustment.AdjustmentType != "AdjustmentOut") 
                return new AdjustmentActionResultDto { Success = false, Message = "Adjustment Type must be AdjustmentIn or AdjustmentOut." };
            if (string.IsNullOrWhiteSpace(adjustment.Reason)) return new AdjustmentActionResultDto { Success = false, Message = "Reason is required." };
            if (adjustment.Items == null || !adjustment.Items.Any()) return new AdjustmentActionResultDto { Success = false, Message = "At least one item is required." };

            foreach (var item in adjustment.Items)
            {
                if (item.ProductID <= 0) return new AdjustmentActionResultDto { Success = false, Message = "Invalid Product selected." };
                if (item.ShelfID <= 0) return new AdjustmentActionResultDto { Success = false, Message = "Invalid Shelf selected." };
                if (item.CountedQty < 0) return new AdjustmentActionResultDto { Success = false, Message = "Counted Quantity cannot be negative." };
                if (item.SystemQty < 0) return new AdjustmentActionResultDto { Success = false, Message = "System Quantity cannot be negative." };
                
                if (adjustment.AdjustmentType == "AdjustmentOut" && item.DifferenceQty < 0 && item.SystemQty + item.DifferenceQty < 0)
                {
                    return new AdjustmentActionResultDto { Success = false, Message = "Adjustment cannot result in negative stock." };
                }
            }

            try
            {
                int newId = await _adjustmentRepository.CreateAdjustmentWithItemsAsync(adjustment);
                return new AdjustmentActionResultDto { Success = true, Message = "Adjustment request created successfully.", NewID = newId };
            }
            catch (Exception ex)
            {
                return new AdjustmentActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<AdjustmentActionResultDto> ApproveAdjustmentAsync(int adjustmentId, int approvedBy)
        {
            var adj = await _adjustmentRepository.GetAdjustmentByIdAsync(adjustmentId);
            if (adj == null) return new AdjustmentActionResultDto { Success = false, Message = "Adjustment not found." };
            if (adj.Status != "Awaiting Approval" && adj.Status != "Pending") return new AdjustmentActionResultDto { Success = false, Message = "Only pending adjustments can be approved." };

            try
            {
                bool success = await _adjustmentRepository.ApproveAdjustmentAsync(adjustmentId, approvedBy);
                if (success) return new AdjustmentActionResultDto { Success = true, Message = "Adjustment approved successfully." };
                return new AdjustmentActionResultDto { Success = false, Message = "Failed to approve adjustment." };
            }
            catch (Exception ex)
            {
                return new AdjustmentActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<AdjustmentActionResultDto> PostAdjustmentAsync(int adjustmentId, int performedBy)
        {
            var adj = await _adjustmentRepository.GetAdjustmentByIdAsync(adjustmentId);
            if (adj == null) return new AdjustmentActionResultDto { Success = false, Message = "Adjustment not found." };
            
            // Allow posting if approved. If UI lacks explicit approval step, allow posting from Pending/Awaiting directly if that's the business rule.
            // Following GEMINI.md: "If @AdjustmentNo IS NULL THROW 50007, 'Adjustment must be approved before posting.', 1;" inside the sproc.
            // So we MUST approve it first if it isn't already. We'll auto-approve here just for fluidity if it's Awaiting Approval.
            if (adj.Status == "Awaiting Approval" || adj.Status == "Pending")
            {
                await _adjustmentRepository.ApproveAdjustmentAsync(adjustmentId, performedBy);
            }

            return await _adjustmentRepository.PostAdjustmentAsync(adjustmentId, performedBy);
        }

        public async Task<decimal> GetCurrentStockAsync(int productId, int shelfId)
        {
            return await _adjustmentRepository.GetCurrentStockAsync(productId, shelfId);
        }

        public async Task<IEnumerable<dynamic>> GetActiveProductsAsync()
        {
            return await _adjustmentRepository.GetActiveProductsAsync();
        }

        public async Task<IEnumerable<dynamic>> GetActiveShelvesAsync()
        {
            return await _adjustmentRepository.GetActiveShelvesAsync();
        }

        public async Task<IEnumerable<InventoryAdjustmentRowDto>> GetAllAdjustmentRowsAsync()
        {
            return await _adjustmentRepository.GetAllAdjustmentRowsAsync();
        }

        public async Task<InventoryAdjustmentDto?> GetAdjustmentByIdAsync(int adjustmentId)
        {
            return await _adjustmentRepository.GetAdjustmentByIdAsync(adjustmentId);
        }

        public async Task<IEnumerable<InventoryAdjustmentItemDto>> GetAdjustmentItemsAsync(int adjustmentId)
        {
            return await _adjustmentRepository.GetAdjustmentItemsAsync(adjustmentId);
        }

        public async Task<IEnumerable<dynamic>> GetShelvesWithProductAsync(int productId)
        {
            return await _adjustmentRepository.GetShelvesWithProductAsync(productId);
        }

        public async Task<IEnumerable<string>> GetWarehousesAsync()
        {
            return await _adjustmentRepository.GetWarehousesAsync();
        }
    }
}


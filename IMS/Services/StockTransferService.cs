using IMS.Models.DTOs;
using IMS.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.Services
{
    public class StockTransferService : IStockTransferService
    {
        private readonly IStockTransferRepository _transferRepository;

        public StockTransferService(IStockTransferRepository transferRepository)
        {
            _transferRepository = transferRepository;
        }

        public async Task<IEnumerable<StockTransferDto>> GetTransfersAsync()
        {
            return await _transferRepository.GetAllTransfersAsync();
        }

        public async Task<IEnumerable<StockTransferItemDto>> GetTransferItemsAsync(int transferId)
        {
            return await _transferRepository.GetTransferItemsAsync(transferId);
        }

        public async Task<StockTransferSummaryDto> GetTransferSummaryAsync()
        {
            var transfers = await _transferRepository.GetAllTransfersAsync();
            var transferList = transfers.ToList();

            return new StockTransferSummaryDto
            {
                TotalTransfers = transferList.Count,
                PendingTransfers = transferList.Count(t => t.Status == "Pending"),
                AwaitingApprovalCount = transferList.Count(t => t.Status == "Awaiting Approval"),
                InTransitCount = transferList.Count(t => t.Status == "In Transit"),
                CompletedCount = transferList.Count(t => t.Status == "Completed")
            };
        }

        public async Task<StockTransferActionResultDto> CreateTransferAsync(StockTransferCreateDto transfer)
        {
            if (transfer.Items == null || !transfer.Items.Any()) return new StockTransferActionResultDto { Success = false, Message = "At least one item is required." };

            transfer.TransferNo = await _transferRepository.GetNextTransferNoAsync();

            foreach (var item in transfer.Items)
            {
                if (item.ProductID <= 0) return new StockTransferActionResultDto { Success = false, Message = "Invalid Product selected." };
                if (item.FromShelfID <= 0) return new StockTransferActionResultDto { Success = false, Message = "Invalid Source Shelf selected." };
                if (item.ToShelfID <= 0) return new StockTransferActionResultDto { Success = false, Message = "Invalid Destination Shelf selected." };
                if (item.FromShelfID == item.ToShelfID) return new StockTransferActionResultDto { Success = false, Message = "Source and Destination shelves cannot be the same." };
                if (item.Quantity <= 0) return new StockTransferActionResultDto { Success = false, Message = "Transfer quantity must be greater than zero." };

                var availableStock = await _transferRepository.GetAvailableStockAsync(item.ProductID, item.FromShelfID);
                if (item.Quantity > availableStock)
                {
                    return new StockTransferActionResultDto { Success = false, Message = $"Insufficient available stock. Requested: {item.Quantity}, Available: {availableStock}." };
                }

                var compatible = await _transferRepository.IsShelfCompatibleForProductAsync(item.ProductID, item.ToShelfID);
                if (!compatible)
                {
                    return new StockTransferActionResultDto { Success = false, Message = "Destination shelf is not compatible with this product storage rule." };
                }

                var quantityCapacity = await _transferRepository.GetShelfAvailableCapacityAsync(item.ToShelfID);
                if (item.Quantity > quantityCapacity)
                {
                    return new StockTransferActionResultDto { Success = false, Message = $"Destination shelf quantity capacity is too low. Requested: {item.Quantity}, Available: {quantityCapacity}." };
                }

                var requiredWeight = await _transferRepository.GetRequiredWeightAsync(item.ProductID, item.Quantity);
                var weightCapacity = await _transferRepository.GetShelfAvailableWeightCapacityAsync(item.ToShelfID);
                if (requiredWeight > weightCapacity)
                {
                    return new StockTransferActionResultDto { Success = false, Message = $"Destination shelf weight capacity is too low. Required: {requiredWeight:N2} kg, Available: {weightCapacity:N2} kg." };
                }
            }

            var firstItem = transfer.Items.First();
            transfer.TransferType = await _transferRepository.DeriveTransferTypeAsync(firstItem.FromShelfID, firstItem.ToShelfID);

            try
            {
                int newId = await _transferRepository.CreateTransferWithItemsAsync(transfer);
                return new StockTransferActionResultDto { Success = true, Message = "Transfer request created successfully.", NewID = newId };
            }
            catch (Exception ex)
            {
                return new StockTransferActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<StockTransferActionResultDto> ApproveTransferAsync(int transferId, int approvedBy)
        {
            var transfer = await _transferRepository.GetTransferByIdAsync(transferId);
            if (transfer == null) return new StockTransferActionResultDto { Success = false, Message = "Transfer not found." };
            if (transfer.Status != "Awaiting Approval" && transfer.Status != "Pending") return new StockTransferActionResultDto { Success = false, Message = "Only pending transfers can be approved." };

            try
            {
                bool success = await _transferRepository.ApproveTransferAsync(transferId, approvedBy);
                if (success) return new StockTransferActionResultDto { Success = true, Message = "Transfer approved successfully." };
                return new StockTransferActionResultDto { Success = false, Message = "Failed to approve transfer." };
            }
            catch (Exception ex)
            {
                return new StockTransferActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<StockTransferActionResultDto> CancelTransferAsync(int transferId, int cancelledBy)
        {
            var transfer = await _transferRepository.GetTransferByIdAsync(transferId);
            if (transfer == null) return new StockTransferActionResultDto { Success = false, Message = "Transfer not found." };
            if (transfer.Status == "Completed" || transfer.Status == "Cancelled") return new StockTransferActionResultDto { Success = false, Message = $"Cannot cancel a transfer in '{transfer.Status}' status." };

            try
            {
                bool success = await _transferRepository.CancelTransferAsync(transferId, cancelledBy);
                if (success) return new StockTransferActionResultDto { Success = true, Message = "Transfer cancelled successfully." };
                return new StockTransferActionResultDto { Success = false, Message = "Failed to cancel transfer." };
            }
            catch (Exception ex)
            {
                return new StockTransferActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<StockTransferActionResultDto> PostTransferAsync(int transferId, int performedBy)
        {
            var transfer = await _transferRepository.GetTransferByIdAsync(transferId);
            if (transfer == null) return new StockTransferActionResultDto { Success = false, Message = "Transfer not found." };
            
            // Allow posting if approved or in transit. Auto approve if awaiting approval.
            if (transfer.Status == "Awaiting Approval" || transfer.Status == "Pending")
            {
                await _transferRepository.ApproveTransferAsync(transferId, performedBy);
            }
            else if (transfer.Status == "Completed")
            {
                 return new StockTransferActionResultDto { Success = false, Message = "Transfer is already completed." };
            }

            return await _transferRepository.PostTransferAsync(transferId, performedBy);
        }

        public async Task<IEnumerable<dynamic>> GetActiveProductsAsync()
        {
            return await _transferRepository.GetActiveProductsAsync();
        }

        public async Task<decimal> GetShelfAvailableWeightCapacityAsync(int shelfId)
        {
            return await _transferRepository.GetShelfAvailableWeightCapacityAsync(shelfId);
        }

        public async Task<IEnumerable<dynamic>> GetActiveShelvesAsync()
        {
            return await _transferRepository.GetActiveShelvesAsync();
        }

        public async Task<IEnumerable<dynamic>> GetShelvesWithProductAsync(int productId)
        {
            return await _transferRepository.GetShelvesWithProductAsync(productId);
        }

        public async Task<IEnumerable<dynamic>> GetProductsByShelfAsync(int shelfId)
        {
            return await _transferRepository.GetProductsByShelfAsync(shelfId);
        }

        public async Task<IEnumerable<ShelfOptionDto>> GetDestinationShelvesForProductAsync(int productId, decimal quantity)
        {
            return await _transferRepository.GetDestinationShelvesForProductAsync(productId, quantity);
        }

        public async Task<ShelfCapacityInfoDto?> GetShelfCapacityInfoAsync(int shelfId, int? productId = null)
        {
            return await _transferRepository.GetShelfCapacityInfoAsync(shelfId, productId);
        }
    }
}

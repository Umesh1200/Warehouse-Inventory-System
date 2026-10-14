using IMS.Models.DTOs;
using IMS.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.Services
{
    public class SupplierReturnService : ISupplierReturnService
    {
        private readonly ISupplierReturnRepository _supplierReturnRepository;
        private readonly IUserContext _userContext;

        public SupplierReturnService(ISupplierReturnRepository supplierReturnRepository, IUserContext userContext)
        {
            _supplierReturnRepository = supplierReturnRepository;
            _userContext = userContext;
        }

        public async Task<IEnumerable<SupplierReturnDto>> GetAllReturnsAsync()
        {
            var returns = await _supplierReturnRepository.GetAllReturnsAsync();
            if (_userContext.SupplierID.HasValue)
            {
                returns = returns.Where(r => r.SupplierID == _userContext.SupplierID.Value);
            }
            return returns;
        }

        public async Task<SupplierReturnDto?> GetReturnByIdAsync(int returnId)
        {
            var ret = await _supplierReturnRepository.GetReturnByIdAsync(returnId);
            if (ret != null && _userContext.SupplierID.HasValue && ret.SupplierID != _userContext.SupplierID.Value)
            {
                return null; // unauthorized
            }
            return ret;
        }

        public async Task<IEnumerable<SupplierReturnItemDto>> GetReturnItemsAsync(int returnId)
        {
            if (_userContext.SupplierID.HasValue)
            {
                var ret = await _supplierReturnRepository.GetReturnByIdAsync(returnId);
                if (ret == null || ret.SupplierID != _userContext.SupplierID.Value)
                {
                    return new List<SupplierReturnItemDto>(); // unauthorized empty list
                }
            }
            return await _supplierReturnRepository.GetReturnItemsAsync(returnId);
        }


        public async Task<SupplierReturnActionResultDto> CreateReturnAsync(SupplierReturnCreateDto returnDto, int createdBy)
        {
            if (returnDto == null)
            {
                return new SupplierReturnActionResultDto { Success = false, Message = "Invalid return data." };
            }

            if (string.IsNullOrWhiteSpace(returnDto.SupplierID) || !int.TryParse(returnDto.SupplierID, out int supplierId))
            {
                return new SupplierReturnActionResultDto { Success = false, Message = "A valid Supplier must be selected." };
            }

            if (returnDto.Items == null || !returnDto.Items.Any())
            {
                return new SupplierReturnActionResultDto { Success = false, Message = "At least one item must be added to the return." };
            }

            // Get available quarantine stock to validate quantities
            var quarantineStock = (await _supplierReturnRepository.GetQuarantineStockAsync()).Cast<dynamic>().ToList();

            foreach (var item in returnDto.Items)
            {
                if (item.ProductID <= 0)
                {
                    return new SupplierReturnActionResultDto { Success = false, Message = "Invalid Product selected." };
                }

                if (item.Quantity <= 0)
                {
                    return new SupplierReturnActionResultDto { Success = false, Message = $"Quantity must be greater than zero for all items." };
                }

                if (item.UnitPrice < 0)
                {
                    return new SupplierReturnActionResultDto { Success = false, Message = "Unit Price cannot be negative." };
                }

                if (item.SourceShelfID <= 0)
                {
                    return new SupplierReturnActionResultDto { Success = false, Message = "A valid Source Shelf (Quarantine) must be specified for each item." };
                }

                // Check quarantine stock availability
                var matchedStock = quarantineStock.FirstOrDefault(q => q.ProductID == item.ProductID && q.ShelfID == item.SourceShelfID);
                if (matchedStock == null)
                {
                    return new SupplierReturnActionResultDto { Success = false, Message = $"Selected item is not in stock in the selected Quarantine Shelf." };
                }

                if ((decimal)matchedStock.CurrentQty < item.Quantity)
                {
                    return new SupplierReturnActionResultDto { Success = false, Message = $"Insufficient stock in selected Quarantine Shelf. Available: {matchedStock.CurrentQty}, Requested: {item.Quantity}." };
                }
            }

            try
            {
                int newId = await _supplierReturnRepository.CreateReturnAsync(returnDto, createdBy);
                return new SupplierReturnActionResultDto { Success = true, Message = "Supplier return created successfully as Draft.", NewID = newId };
            }
            catch (Exception ex)
            {
                return new SupplierReturnActionResultDto { Success = false, Message = $"Failed to create supplier return: {ex.Message}" };
            }
        }

        public async Task<SupplierReturnActionResultDto> ConfirmReturnAsync(int returnId, int performedBy)
        {
            if (returnId <= 0)
            {
                return new SupplierReturnActionResultDto { Success = false, Message = "Invalid Return ID." };
            }

            var returnHeader = await _supplierReturnRepository.GetReturnByIdAsync(returnId);
            if (returnHeader == null)
            {
                return new SupplierReturnActionResultDto { Success = false, Message = "Supplier return not found." };
            }

            if (returnHeader.Status != "Draft")
            {
                return new SupplierReturnActionResultDto { Success = false, Message = $"Only Draft returns can be confirmed. Current status: {returnHeader.Status}." };
            }

            // Fetch return items to ensure we still have sufficient stock in Quarantine
            var returnItems = await _supplierReturnRepository.GetReturnItemsAsync(returnId);
            var quarantineStock = (await _supplierReturnRepository.GetQuarantineStockAsync()).Cast<dynamic>().ToList();

            foreach (var item in returnItems)
            {
                var matchedStock = quarantineStock.FirstOrDefault(q => q.ProductID == item.ProductID && q.ShelfID == item.SourceShelfID);
                if (matchedStock == null)
                {
                    return new SupplierReturnActionResultDto { Success = false, Message = $"Cannot confirm return. Insufficient stock in Quarantine shelf {item.SourceShelfCode} for product {item.SKU}. Available: 0, Return Qty: {item.Quantity}." };
                }

                decimal currentQty = (decimal)matchedStock.CurrentQty;
                if (currentQty < item.Quantity)
                {
                    return new SupplierReturnActionResultDto { Success = false, Message = $"Cannot confirm return. Insufficient stock in Quarantine shelf {item.SourceShelfCode} for product {item.SKU}. Available: {currentQty}, Return Qty: {item.Quantity}." };
                }
            }


            try
            {
                bool success = await _supplierReturnRepository.ConfirmReturnAsync(returnId, performedBy);
                if (success)
                {
                    return new SupplierReturnActionResultDto { Success = true, Message = "Supplier return confirmed and inventory deducted successfully." };
                }
                else
                {
                    return new SupplierReturnActionResultDto { Success = false, Message = "Failed to confirm supplier return." };
                }
            }
            catch (Exception ex)
            {
                return new SupplierReturnActionResultDto { Success = false, Message = $"Error confirming supplier return: {ex.Message}" };
            }
        }

        public async Task<SupplierReturnActionResultDto> UpdateCreditStatusAsync(int returnId, string creditStatus)
        {
            if (returnId <= 0)
            {
                return new SupplierReturnActionResultDto { Success = false, Message = "Invalid Return ID." };
            }

            if (creditStatus != "Pending" && creditStatus != "Issued" && creditStatus != "Denied")
            {
                return new SupplierReturnActionResultDto { Success = false, Message = "Invalid Credit Status value. Allowed: Pending, Issued, Denied." };
            }

            try
            {
                bool success = await _supplierReturnRepository.UpdateCreditStatusAsync(returnId, creditStatus);
                if (success)
                {
                    return new SupplierReturnActionResultDto { Success = true, Message = $"Credit status updated to '{creditStatus}' successfully." };
                }
                else
                {
                    return new SupplierReturnActionResultDto { Success = false, Message = "Supplier return not found or status could not be updated." };
                }
            }
            catch (Exception ex)
            {
                return new SupplierReturnActionResultDto { Success = false, Message = $"Error updating credit status: {ex.Message}" };
            }
        }

        public async Task<SupplierReturnActionResultDto> CancelReturnAsync(int returnId)
        {
            if (returnId <= 0)
            {
                return new SupplierReturnActionResultDto { Success = false, Message = "Invalid Return ID." };
            }

            try
            {
                bool success = await _supplierReturnRepository.CancelReturnAsync(returnId);
                if (success)
                {
                    return new SupplierReturnActionResultDto { Success = true, Message = "Supplier return cancelled successfully." };
                }
                else
                {
                    return new SupplierReturnActionResultDto { Success = false, Message = "Only Draft returns can be cancelled." };
                }
            }
            catch (Exception ex)
            {
                return new SupplierReturnActionResultDto { Success = false, Message = $"Error cancelling supplier return: {ex.Message}" };
            }
        }

        public async Task<SupplierReturnSummaryDto> GetReturnsSummaryAsync()
        {
            if (_userContext.SupplierID.HasValue)
            {
                var returns = await GetAllReturnsAsync();
                var returnsList = returns.ToList();
                
                decimal totalValue = 0;
                int pendingCredit = 0;
                int shippedReturns = 0;
                
                foreach (var r in returnsList)
                {
                    var items = await _supplierReturnRepository.GetReturnItemsAsync(r.ReturnID);
                    totalValue += items.Sum(i => i.Quantity * i.UnitPrice);
                    if (r.CreditStatus == "Pending" && r.Status == "Shipped") pendingCredit++;
                    if (r.Status == "Shipped") shippedReturns++;
                }

                return new SupplierReturnSummaryDto
                {
                    TotalReturns = returnsList.Count,
                    PendingCredit = pendingCredit,
                    ShippedReturns = shippedReturns,
                    TotalValue = totalValue
                };
            }
            return await _supplierReturnRepository.GetReturnsSummaryAsync();
        }

        public async Task<IEnumerable<dynamic>> GetQuarantineStockAsync()
        {
            if (_userContext.SupplierID.HasValue)
            {
                return new List<dynamic>(); // suppliers cannot see quarantine stock
            }
            return await _supplierReturnRepository.GetQuarantineStockAsync();
        }

        public async Task<SupplierReturnActionResultDto> SupplierAuthorizeReturnAsync(int returnId, string rmaNumber, string carrier, string trackingNumber, string? creditNoteNo = null, decimal? creditAmount = null, string? replacementSKU = null, decimal? replacementQty = null, string? replacementTracking = null)
        {
            if (returnId <= 0) return new SupplierReturnActionResultDto { Success = false, Message = "Invalid Return ID." };
            if (string.IsNullOrWhiteSpace(rmaNumber)) return new SupplierReturnActionResultDto { Success = false, Message = "RMA Number is required." };

            if (_userContext.SupplierID.HasValue)
            {
                var ret = await _supplierReturnRepository.GetReturnByIdAsync(returnId);
                if (ret == null || ret.SupplierID != _userContext.SupplierID.Value)
                {
                    return new SupplierReturnActionResultDto { Success = false, Message = "Unauthorized return access." };
                }
            }

            try
            {
                bool success = await _supplierReturnRepository.SupplierAuthorizeReturnAsync(returnId, rmaNumber, carrier, trackingNumber, creditNoteNo, creditAmount, replacementSKU, replacementQty, replacementTracking);
                if (success)
                {
                    return new SupplierReturnActionResultDto { Success = true, Message = "Return authorized with RMA details successfully." };
                }
                return new SupplierReturnActionResultDto { Success = false, Message = "Failed to authorize return." };
            }
            catch (Exception ex)
            {
                return new SupplierReturnActionResultDto { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<SupplierReturnActionResultDto> RejectSupplierReturnAsync(int returnId, string rejectionReason)
        {
            if (returnId <= 0) return new SupplierReturnActionResultDto { Success = false, Message = "Invalid Return ID." };
            if (string.IsNullOrWhiteSpace(rejectionReason)) return new SupplierReturnActionResultDto { Success = false, Message = "Rejection/Dispute reason is required." };

            if (_userContext.SupplierID.HasValue)
            {
                var ret = await _supplierReturnRepository.GetReturnByIdAsync(returnId);
                if (ret == null || ret.SupplierID != _userContext.SupplierID.Value)
                {
                    return new SupplierReturnActionResultDto { Success = false, Message = "Unauthorized return access." };
                }
            }

            try
            {
                var ret = await _supplierReturnRepository.GetReturnByIdAsync(returnId);
                if (ret == null || ret.Status != "Draft")
                {
                    return new SupplierReturnActionResultDto { Success = false, Message = "Only draft return claims can be rejected." };
                }

                bool success = await _supplierReturnRepository.RejectSupplierReturnAsync(returnId, rejectionReason);
                if (success)
                {
                    return new SupplierReturnActionResultDto { Success = true, Message = "Return claim has been rejected/disputed." };
                }
                return new SupplierReturnActionResultDto { Success = false, Message = "Failed to reject return claim." };
            }
            catch (Exception ex)
            {
                return new SupplierReturnActionResultDto { Success = false, Message = $"Error: {ex.Message}" };
            }
        }
    }
}

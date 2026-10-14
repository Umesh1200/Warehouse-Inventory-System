using IMS.Models.DTOs;
using IMS.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.Services
{
    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly IPurchaseOrderRepository _poRepository;
        private readonly IUserContext _userContext;

        public PurchaseOrderService(IPurchaseOrderRepository poRepository, IUserContext userContext)
        {
            _poRepository = poRepository;
            _userContext = userContext;
        }

        public async Task<IEnumerable<PurchaseOrderDto>> GetPurchaseOrdersAsync()
        {
            var pos = await _poRepository.GetAllPurchaseOrdersAsync();
            if (_userContext.SupplierID.HasValue)
            {
                pos = pos.Where(o => o.SupplierID == _userContext.SupplierID.Value);
            }
            return pos;
        }

        public async Task<IEnumerable<PurchaseOrderItemDto>> GetPurchaseOrderItemsAsync(int poid)
        {
            if (_userContext.SupplierID.HasValue)
            {
                var pos = await _poRepository.GetAllPurchaseOrdersAsync();
                var hasAccess = pos.Any(o => o.POID == poid && o.SupplierID == _userContext.SupplierID.Value);
                if (!hasAccess)
                {
                    return new List<PurchaseOrderItemDto>(); // unauthorized empty list
                }
            }
            return await _poRepository.GetPurchaseOrderItemsAsync(poid);
        }


        public async Task<PurchaseOrderSummaryDto> GetPurchaseOrderSummaryAsync()
        {
            var pos = await _poRepository.GetAllPurchaseOrdersAsync();
            var poList = pos.ToList();
            
            if (_userContext.SupplierID.HasValue)
            {
                poList = poList.Where(o => o.SupplierID == _userContext.SupplierID.Value).ToList();
            }

            return new PurchaseOrderSummaryDto
            {
                TotalOrders = poList.Count,
                DraftOrders = poList.Count(o => o.Status == "Draft"),
                PendingConfirmedOrders = poList.Count(o => o.Status == "Confirmed"),
                InTransitOrders = poList.Count(o => o.Status == "InTransit"),
                ProcessingOrders = poList.Count(o => new[] { "Received", "QCCheck", "Putaway" }.Contains(o.Status))
            };
        }


        public async Task<PurchaseOrderActionResultDto> CreatePurchaseOrderAsync(PurchaseOrderCreateDto po)
        {
            if (po.SupplierID <= 0) return new PurchaseOrderActionResultDto { Success = false, Message = "Supplier is required." };
            if (po.ExpectedDate.HasValue && po.ExpectedDate.Value < po.OrderDate) return new PurchaseOrderActionResultDto { Success = false, Message = "Expected Date cannot be before Order Date." };
            if (po.Items == null || !po.Items.Any()) return new PurchaseOrderActionResultDto { Success = false, Message = "At least one item is required." };

            foreach (var item in po.Items)
            {
                if (item.ProductID <= 0) return new PurchaseOrderActionResultDto { Success = false, Message = "Invalid Product selected." };
                if (item.OrderedQty <= 0) return new PurchaseOrderActionResultDto { Success = false, Message = "Ordered quantity must be greater than zero." };
                if (item.UnitPrice < 0) return new PurchaseOrderActionResultDto { Success = false, Message = "Unit price cannot be negative." };
                if (!await _poRepository.IsSupplierProductApprovedAsync(po.SupplierID, item.ProductID))
                {
                    return new PurchaseOrderActionResultDto { Success = false, Message = "Selected product is not approved for this supplier." };
                }
            }

            if (!string.IsNullOrWhiteSpace(po.PONumber) && await _poRepository.IsPONumberExistsAsync(po.PONumber))
            {
                return new PurchaseOrderActionResultDto { Success = false, Message = $"PO Number '{po.PONumber}' already exists." };
            }

            try
            {
                int newId = await _poRepository.CreatePurchaseOrderWithItemsAsync(po);
                return new PurchaseOrderActionResultDto { Success = true, Message = "Purchase Order created successfully.", NewID = newId };
            }
            catch (Exception ex)
            {
                return new PurchaseOrderActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<PurchaseOrderActionResultDto> UpdatePurchaseOrderAsync(PurchaseOrderUpdateDto po)
        {
            if (po.POID <= 0) return new PurchaseOrderActionResultDto { Success = false, Message = "Invalid PO ID." };
            if (string.IsNullOrWhiteSpace(po.PONumber)) return new PurchaseOrderActionResultDto { Success = false, Message = "PO Number is required." };
            if (po.SupplierID <= 0) return new PurchaseOrderActionResultDto { Success = false, Message = "Supplier is required." };
            if (po.ExpectedDate.HasValue && po.ExpectedDate.Value < po.OrderDate) return new PurchaseOrderActionResultDto { Success = false, Message = "Expected Date cannot be before Order Date." };
            if (po.Items == null || !po.Items.Any()) return new PurchaseOrderActionResultDto { Success = false, Message = "At least one item is required." };

            foreach (var item in po.Items)
            {
                if (item.ProductID <= 0) return new PurchaseOrderActionResultDto { Success = false, Message = "Invalid Product selected." };
                if (item.OrderedQty <= 0) return new PurchaseOrderActionResultDto { Success = false, Message = "Ordered quantity must be greater than zero." };
                if (item.UnitPrice < 0) return new PurchaseOrderActionResultDto { Success = false, Message = "Unit price cannot be negative." };
                if (!await _poRepository.IsSupplierProductApprovedAsync(po.SupplierID, item.ProductID))
                {
                    return new PurchaseOrderActionResultDto { Success = false, Message = "Selected product is not approved for this supplier." };
                }
            }

            var existingPo = await _poRepository.GetPurchaseOrderByIdAsync(po.POID);
            if (existingPo == null) return new PurchaseOrderActionResultDto { Success = false, Message = "Purchase Order not found." };
            if (existingPo.Status != "Draft") return new PurchaseOrderActionResultDto { Success = false, Message = "Only Draft Purchase Orders can be edited." };

            if (await _poRepository.IsPONumberExistsForOtherPOAsync(po.PONumber, po.POID))
            {
                return new PurchaseOrderActionResultDto { Success = false, Message = $"PO Number '{po.PONumber}' already exists." };
            }

            try
            {
                bool success = await _poRepository.UpdatePurchaseOrderWithItemsAsync(po);
                if (success)
                    return new PurchaseOrderActionResultDto { Success = true, Message = "Purchase Order updated successfully." };
                else
                    return new PurchaseOrderActionResultDto { Success = false, Message = "Failed to update Purchase Order. It may no longer be a Draft." };
            }
            catch (Exception ex)
            {
                return new PurchaseOrderActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<PurchaseOrderActionResultDto> ConfirmPurchaseOrderAsync(int poId, int approvedBy)
        {
            var existingPo = await _poRepository.GetPurchaseOrderByIdAsync(poId);
            if (existingPo == null) return new PurchaseOrderActionResultDto { Success = false, Message = "Purchase Order not found." };
            if (existingPo.Status != "Draft" && existingPo.Status != "Pending Approval") 
                return new PurchaseOrderActionResultDto { Success = false, Message = "Only Draft or Pending Approval Purchase Orders can be approved." };

            try
            {
                bool success = await _poRepository.ConfirmPurchaseOrderAsync(poId, approvedBy);
                if (success) return new PurchaseOrderActionResultDto { Success = true, Message = "Purchase Order approved and confirmed successfully." };
                return new PurchaseOrderActionResultDto { Success = false, Message = "Failed to approve Purchase Order." };
            }
            catch (Exception ex)
            {
                return new PurchaseOrderActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<PurchaseOrderActionResultDto> SubmitForApprovalAsync(int poId, int submittedBy)
        {
            var existingPo = await _poRepository.GetPurchaseOrderByIdAsync(poId);
            if (existingPo == null) return new PurchaseOrderActionResultDto { Success = false, Message = "Purchase Order not found." };
            if (existingPo.Status != "Draft" && existingPo.Status != "Rejected") 
                return new PurchaseOrderActionResultDto { Success = false, Message = "Only Draft or Rejected Purchase Orders can be submitted for approval." };

            try
            {
                bool success = await _poRepository.SubmitForApprovalAsync(poId, submittedBy);
                if (success) return new PurchaseOrderActionResultDto { Success = true, Message = "Purchase Order submitted for approval successfully." };
                return new PurchaseOrderActionResultDto { Success = false, Message = "Failed to submit Purchase Order for approval." };
            }
            catch (Exception ex)
            {
                return new PurchaseOrderActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<PurchaseOrderActionResultDto> RejectApprovalAsync(int poId, int rejectedBy, string rejectionReason)
        {
            var existingPo = await _poRepository.GetPurchaseOrderByIdAsync(poId);
            if (existingPo == null) return new PurchaseOrderActionResultDto { Success = false, Message = "Purchase Order not found." };
            if (existingPo.Status != "Draft" && existingPo.Status != "Pending Approval") 
                return new PurchaseOrderActionResultDto { Success = false, Message = "Only Draft or Pending Approval Purchase Orders can be rejected." };

            try
            {
                bool success = await _poRepository.RejectApprovalAsync(poId, rejectedBy, rejectionReason);
                if (success) return new PurchaseOrderActionResultDto { Success = true, Message = "Purchase Order rejected." };
                return new PurchaseOrderActionResultDto { Success = false, Message = "Failed to reject Purchase Order." };
            }
            catch (Exception ex)
            {
                return new PurchaseOrderActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<PurchaseOrderActionResultDto> CancelPurchaseOrderAsync(int poId)
        {
            var existingPo = await _poRepository.GetPurchaseOrderByIdAsync(poId);
            if (existingPo == null) return new PurchaseOrderActionResultDto { Success = false, Message = "Purchase Order not found." };
            if (existingPo.Status != "Draft" && existingPo.Status != "Pending Approval" && existingPo.Status != "Confirmed") 
                return new PurchaseOrderActionResultDto { Success = false, Message = "Only Draft, Pending Approval, or Confirmed Purchase Orders can be cancelled." };

            try
            {
                bool success = await _poRepository.CancelPurchaseOrderAsync(poId);
                if (success) return new PurchaseOrderActionResultDto { Success = true, Message = "Purchase Order cancelled successfully." };
                return new PurchaseOrderActionResultDto { Success = false, Message = "Failed to cancel Purchase Order." };
            }
            catch (Exception ex)
            {
                return new PurchaseOrderActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<IEnumerable<dynamic>> GetActiveSuppliersAsync()
        {
            var suppliers = await _poRepository.GetActiveSuppliersAsync();
            if (_userContext.SupplierID.HasValue)
            {
                suppliers = suppliers.Where(s => s.ID == _userContext.SupplierID.Value);
            }
            return suppliers;
        }

        public async Task<IEnumerable<dynamic>> GetActiveProductsAsync()
        {
            return await _poRepository.GetActiveProductsAsync();
        }

        public async Task<IEnumerable<SupplierProductDto>> GetSupplierProductMappingsAsync()
        {
            var mappings = await _poRepository.GetSupplierProductMappingsAsync();
            if (_userContext.SupplierID.HasValue)
            {
                mappings = mappings.Where(m => m.SupplierID == _userContext.SupplierID.Value);
            }
            return mappings;
        }


        public async Task<PurchaseOrderActionResultDto> SaveSupplierProductMappingAsync(SupplierProductDto mapping)
        {
            if (mapping.SupplierID <= 0) return new PurchaseOrderActionResultDto { Success = false, Message = "Supplier is required." };
            if (mapping.ProductID <= 0) return new PurchaseOrderActionResultDto { Success = false, Message = "Product is required." };
            if (mapping.LastQuotedPrice < 0) return new PurchaseOrderActionResultDto { Success = false, Message = "Quoted price cannot be negative." };
            if (mapping.LeadTimeDays < 0) return new PurchaseOrderActionResultDto { Success = false, Message = "Lead time cannot be negative." };

            mapping.Status = string.IsNullOrWhiteSpace(mapping.Status) ? "Approved" : mapping.Status;
            mapping.MinOrderQty = mapping.MinOrderQty <= 0 ? 1 : mapping.MinOrderQty;
            mapping.PackSize = mapping.PackSize <= 0 ? 1 : mapping.PackSize;

            try
            {
                var id = await _poRepository.UpsertSupplierProductAsync(mapping);
                return new PurchaseOrderActionResultDto { Success = true, Message = "Approved vendor mapping saved.", NewID = id };
            }
            catch (Exception ex)
            {
                return new PurchaseOrderActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<PurchaseOrderActionResultDto> BlockSupplierProductMappingAsync(int supplierProductId)
        {
            if (supplierProductId <= 0) return new PurchaseOrderActionResultDto { Success = false, Message = "Invalid mapping ID." };

            try
            {
                var success = await _poRepository.SetSupplierProductStatusAsync(supplierProductId, "Blocked");
                return success
                    ? new PurchaseOrderActionResultDto { Success = true, Message = "Approved vendor mapping blocked." }
                    : new PurchaseOrderActionResultDto { Success = false, Message = "Mapping not found." };
            }
            catch (Exception ex)
            {
                return new PurchaseOrderActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<Dictionary<int, ComponentProcurementDto>> GetProcurementDataForProductsAsync(List<int> productIds)
        {
            return await _poRepository.GetProcurementDataForProductsAsync(productIds);
        }

        public async Task<PurchaseOrderActionResultDto> ShipPurchaseOrderAsync(int poId, int supplierId, string carrier, string trackingNumber, DateTime expectedDate, Dictionary<int, decimal> itemShippedQuantities)
        {
            if (poId <= 0) return new PurchaseOrderActionResultDto { Success = false, Message = "Invalid Purchase Order ID." };
            if (string.IsNullOrWhiteSpace(carrier)) return new PurchaseOrderActionResultDto { Success = false, Message = "Shipping Carrier is required." };
            if (string.IsNullOrWhiteSpace(trackingNumber)) return new PurchaseOrderActionResultDto { Success = false, Message = "Tracking Number is required." };
            if (expectedDate < DateTime.Today.AddDays(-1)) return new PurchaseOrderActionResultDto { Success = false, Message = "Estimated delivery date cannot be in the past." };
            if (itemShippedQuantities == null || !itemShippedQuantities.Any()) return new PurchaseOrderActionResultDto { Success = false, Message = "Shipped quantities must be provided for PO line items." };

            foreach (var kvp in itemShippedQuantities)
            {
                if (kvp.Value < 0) return new PurchaseOrderActionResultDto { Success = false, Message = "Shipped quantity cannot be negative." };
            }

            try
            {
                bool success = await _poRepository.ShipPurchaseOrderAsync(poId, supplierId, carrier, trackingNumber, expectedDate, itemShippedQuantities);
                if (success)
                {
                    return new PurchaseOrderActionResultDto { Success = true, Message = "Purchase Order marked as Shipped successfully." };
                }
                return new PurchaseOrderActionResultDto { Success = false, Message = "Unable to ship PO. It must be in Confirmed status." };
            }
            catch (Exception ex)
            {
                return new PurchaseOrderActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<PurchaseOrderActionResultDto> RejectPurchaseOrderAsync(int poId, int supplierId, string rejectionReason)
        {
            if (poId <= 0) return new PurchaseOrderActionResultDto { Success = false, Message = "Invalid Purchase Order ID." };
            if (string.IsNullOrWhiteSpace(rejectionReason)) return new PurchaseOrderActionResultDto { Success = false, Message = "Rejection reason is required." };

            try
            {
                var po = await _poRepository.GetPurchaseOrderByIdAsync(poId);
                if (po == null || po.SupplierID != supplierId)
                {
                    return new PurchaseOrderActionResultDto { Success = false, Message = "Unauthorized access or Purchase Order not found." };
                }

                if (po.Status != "Confirmed")
                {
                    return new PurchaseOrderActionResultDto { Success = false, Message = "Only Confirmed Purchase Orders can be rejected." };
                }

                bool success = await _poRepository.RejectPurchaseOrderAsync(poId, rejectionReason);
                if (success)
                {
                    return new PurchaseOrderActionResultDto { Success = true, Message = "Purchase Order has been rejected." };
                }
                return new PurchaseOrderActionResultDto { Success = false, Message = "Failed to reject Purchase Order." };
            }
            catch (Exception ex)
            {
                return new PurchaseOrderActionResultDto { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<PurchaseOrderDto?> GetPurchaseOrderDetailsAsync(int poId)
        {
            if (_userContext.SupplierID.HasValue)
            {
                var po = await _poRepository.GetPurchaseOrderDetailsAsync(poId);
                if (po != null && po.SupplierID == _userContext.SupplierID.Value)
                {
                    return po;
                }
                return null;
            }
            return await _poRepository.GetPurchaseOrderDetailsAsync(poId);
        }

        public async Task<(IEnumerable<PurchaseOrderDto> Orders, int TotalCount)> GetPagedPurchaseOrdersAsync(
            int offset, int limit, string? search, string? supplier, string? status)
        {
            int? supplierIdScope = null;
            if (_userContext.SupplierID.HasValue)
            {
                supplierIdScope = _userContext.SupplierID.Value;
            }

            return await _poRepository.GetPagedPurchaseOrdersAsync(offset, limit, search, supplier, status, supplierIdScope);
        }
    }
}

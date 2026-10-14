using IMS.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public interface IPurchaseOrderRepository
    {
        Task<IEnumerable<PurchaseOrderDto>> GetAllPurchaseOrdersAsync();
        Task<IEnumerable<PurchaseOrderItemDto>> GetPurchaseOrderItemsAsync(int poid);
        Task<int> CreatePurchaseOrderWithItemsAsync(PurchaseOrderCreateDto po);
        Task<bool> UpdatePurchaseOrderWithItemsAsync(PurchaseOrderUpdateDto po);
        Task<bool> ConfirmPurchaseOrderAsync(int poId, int approvedBy);
        Task<bool> SubmitForApprovalAsync(int poId, int submittedBy);
        Task<bool> RejectApprovalAsync(int poId, int rejectedBy, string rejectionReason);
        Task<bool> CancelPurchaseOrderAsync(int poId);
        Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(int poId);
        Task<bool> IsPONumberExistsAsync(string poNumber);
        Task<bool> IsPONumberExistsForOtherPOAsync(string poNumber, int poId);
        Task<IEnumerable<dynamic>> GetActiveSuppliersAsync();
        Task<IEnumerable<dynamic>> GetActiveProductsAsync();
        Task<IEnumerable<SupplierProductDto>> GetSupplierProductMappingsAsync();
        Task<bool> IsSupplierProductApprovedAsync(int supplierId, int productId);
        Task<int> UpsertSupplierProductAsync(SupplierProductDto mapping);
        Task<bool> SetSupplierProductStatusAsync(int supplierProductId, string status);
        Task<Dictionary<int, ComponentProcurementDto>> GetProcurementDataForProductsAsync(List<int> productIds);
        Task<bool> ShipPurchaseOrderAsync(int poId, int supplierId, string carrier, string trackingNumber, DateTime expectedDate, Dictionary<int, decimal> itemShippedQuantities);
        Task<bool> RejectPurchaseOrderAsync(int poId, string rejectionReason);
        Task<PurchaseOrderDto?> GetPurchaseOrderDetailsAsync(int poId);
        Task<(IEnumerable<PurchaseOrderDto> Orders, int TotalCount)> GetPagedPurchaseOrdersAsync(int offset, int limit, string? search, string? supplier, string? status, int? supplierIdScope = null);
    }
}

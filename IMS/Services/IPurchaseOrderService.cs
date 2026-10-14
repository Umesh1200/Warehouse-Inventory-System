using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Services
{
    public interface IPurchaseOrderService
    {
        Task<IEnumerable<PurchaseOrderDto>> GetPurchaseOrdersAsync();
        Task<IEnumerable<PurchaseOrderItemDto>> GetPurchaseOrderItemsAsync(int poid);
        Task<PurchaseOrderSummaryDto> GetPurchaseOrderSummaryAsync();
        Task<PurchaseOrderActionResultDto> CreatePurchaseOrderAsync(PurchaseOrderCreateDto po);
        Task<PurchaseOrderActionResultDto> UpdatePurchaseOrderAsync(PurchaseOrderUpdateDto po);
        Task<PurchaseOrderActionResultDto> ConfirmPurchaseOrderAsync(int poId, int approvedBy);
        Task<PurchaseOrderActionResultDto> SubmitForApprovalAsync(int poId, int submittedBy);
        Task<PurchaseOrderActionResultDto> RejectApprovalAsync(int poId, int rejectedBy, string rejectionReason);
        Task<PurchaseOrderActionResultDto> CancelPurchaseOrderAsync(int poId);
        Task<IEnumerable<dynamic>> GetActiveSuppliersAsync();
        Task<IEnumerable<dynamic>> GetActiveProductsAsync();
        Task<IEnumerable<SupplierProductDto>> GetSupplierProductMappingsAsync();
        Task<PurchaseOrderActionResultDto> SaveSupplierProductMappingAsync(SupplierProductDto mapping);
        Task<PurchaseOrderActionResultDto> BlockSupplierProductMappingAsync(int supplierProductId);
        Task<Dictionary<int, ComponentProcurementDto>> GetProcurementDataForProductsAsync(List<int> productIds);
        Task<PurchaseOrderActionResultDto> ShipPurchaseOrderAsync(int poId, int supplierId, string carrier, string trackingNumber, DateTime expectedDate, Dictionary<int, decimal> itemShippedQuantities);
        Task<PurchaseOrderActionResultDto> RejectPurchaseOrderAsync(int poId, int supplierId, string rejectionReason);
        Task<(IEnumerable<PurchaseOrderDto> Orders, int TotalCount)> GetPagedPurchaseOrdersAsync(int offset, int limit, string? search, string? supplier, string? status);
        Task<PurchaseOrderDto?> GetPurchaseOrderDetailsAsync(int poId);
    }
}

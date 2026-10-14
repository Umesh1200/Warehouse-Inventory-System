using IMS.Models.DTOs;

namespace IMS.Repositories;

public interface IProcurementWorkflowRepository
{
    Task<IReadOnlyList<ProcurementOrderWorkflowDto>> GetOrdersAsync();
    Task<ProcurementOrderWorkflowDto?> GetOrderAsync(int poId);
    Task<int> AddCommunicationAsync(POCommunicationDto communication);
    Task<int> AddConfirmationAsync(SupplierConfirmationDto confirmation);
    Task<int> AddDocumentAsync(ProcurementDocumentDto document);
    Task<ProcurementDocumentDto?> GetDocumentAsync(int documentId);
    Task<IReadOnlyList<SupplierInvoiceDto>> GetInvoicesAsync();
    Task<int> CreateInvoiceAsync(SupplierInvoiceCreateDto invoice);
    Task<bool> SetInvoiceStatusAsync(int invoiceId, string status, int userId);
    Task<int> AddPaymentAsync(SupplierPaymentCreateDto payment);
}

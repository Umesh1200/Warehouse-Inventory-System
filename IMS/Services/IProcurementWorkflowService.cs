using IMS.Models.DTOs;

namespace IMS.Services;

public interface IProcurementWorkflowService
{
    Task<IReadOnlyList<ProcurementOrderWorkflowDto>> GetOrdersAsync();
    Task<ProcurementOrderWorkflowDto?> GetOrderAsync(int poId);
    Task<ProcurementActionResultDto> RecordCommunicationAsync(POCommunicationDto communication);
    Task<ProcurementActionResultDto> RecordConfirmationAsync(SupplierConfirmationDto confirmation);
    Task<ProcurementActionResultDto> AddDocumentAsync(ProcurementDocumentDto document);
    Task<ProcurementDocumentDto?> GetDocumentAsync(int documentId);
    Task<IReadOnlyList<SupplierInvoiceDto>> GetInvoicesAsync();
    Task<ProcurementActionResultDto> CreateInvoiceAsync(SupplierInvoiceCreateDto invoice);
    Task<ProcurementActionResultDto> SetInvoiceStatusAsync(int invoiceId, string status, int userId);
    Task<ProcurementActionResultDto> RecordPaymentAsync(SupplierPaymentCreateDto payment);
}

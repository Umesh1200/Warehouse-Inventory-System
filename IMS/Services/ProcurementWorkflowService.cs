using IMS.Models.DTOs;
using IMS.Repositories;

namespace IMS.Services;

public class ProcurementWorkflowService : IProcurementWorkflowService
{
    private static readonly string[] Channels = ["Email", "Phone", "Messaging", "Print", "Other"];
    private static readonly string[] ConfirmationStatuses = ["Pending", "Confirmed", "Declined", "Changes Requested"];
    private static readonly string[] InvoiceStatuses = ["Approved", "On Hold", "Rejected"];
    private readonly IProcurementWorkflowRepository _repository;

    public ProcurementWorkflowService(IProcurementWorkflowRepository repository) => _repository = repository;

    public Task<IReadOnlyList<ProcurementOrderWorkflowDto>> GetOrdersAsync() => _repository.GetOrdersAsync();
    public Task<ProcurementOrderWorkflowDto?> GetOrderAsync(int poId) => _repository.GetOrderAsync(poId);
    public Task<ProcurementDocumentDto?> GetDocumentAsync(int documentId) => _repository.GetDocumentAsync(documentId);
    public Task<IReadOnlyList<SupplierInvoiceDto>> GetInvoicesAsync() => _repository.GetInvoicesAsync();

    public async Task<ProcurementActionResultDto> RecordCommunicationAsync(POCommunicationDto value)
    {
        if (value.POID <= 0 || value.RecordedBy <= 0) return Fail("A valid PO and staff user are required.");
        if (!Channels.Contains(value.Channel)) return Fail("Select a valid communication channel.");
        if (value.CommunicatedAt > DateTime.Now.AddMinutes(5)) return Fail("Communication time cannot be in the future.");
        return await Insert(() => _repository.AddCommunicationAsync(value), "Communication recorded.");
    }

    public async Task<ProcurementActionResultDto> RecordConfirmationAsync(SupplierConfirmationDto value)
    {
        if (value.POID <= 0 || value.RecordedBy <= 0) return Fail("A valid PO and staff user are required.");
        if (!ConfirmationStatuses.Contains(value.ConfirmationStatus)) return Fail("Select a valid supplier response.");
        if (value.ConfirmedAt > DateTime.Now.AddMinutes(5)) return Fail("Response time cannot be in the future.");
        if (value.PromisedDeliveryDate.HasValue && value.PromisedDeliveryDate.Value.Date < value.ConfirmedAt.Date)
            return Fail("Promised delivery cannot be before the supplier response date.");
        return await Insert(() => _repository.AddConfirmationAsync(value), "Supplier response recorded.");
    }

    public async Task<ProcurementActionResultDto> AddDocumentAsync(ProcurementDocumentDto value)
    {
        if (value.POID <= 0 || value.UploadedBy <= 0 || string.IsNullOrWhiteSpace(value.StoredFileName)) return Fail("Invalid document metadata.");
        return await Insert(() => _repository.AddDocumentAsync(value), "Document uploaded.");
    }

    public async Task<ProcurementActionResultDto> CreateInvoiceAsync(SupplierInvoiceCreateDto value)
    {
        if (value.POID <= 0 || value.RecordedBy <= 0) return Fail("A valid PO and staff user are required.");
        if (string.IsNullOrWhiteSpace(value.InvoiceNumber)) return Fail("Invoice number is required.");
        if (value.TotalAmount <= 0 || value.Subtotal < 0 || value.TaxAmount < 0) return Fail("Invoice amounts are invalid.");
        if (Math.Abs(value.TotalAmount - (value.Subtotal + value.TaxAmount)) > 0.01m) return Fail("Total must equal subtotal plus tax.");
        if (value.DueDate.HasValue && value.DueDate.Value.Date < value.InvoiceDate.Date) return Fail("Due date cannot be before invoice date.");
        return await Insert(() => _repository.CreateInvoiceAsync(value), "Supplier invoice recorded.");
    }

    public async Task<ProcurementActionResultDto> SetInvoiceStatusAsync(int invoiceId, string status, int userId)
    {
        if (invoiceId <= 0 || userId <= 0 || !InvoiceStatuses.Contains(status)) return Fail("Invalid invoice action.");
        try
        {
            return await _repository.SetInvoiceStatusAsync(invoiceId, status, userId)
                ? new() { Success = true, Message = $"Invoice marked {status}." }
                : Fail("Invoice was not found.");
        }
        catch (Exception ex) { return Fail(ex.Message); }
    }

    public async Task<ProcurementActionResultDto> RecordPaymentAsync(SupplierPaymentCreateDto value)
    {
        if (value.InvoiceID <= 0 || value.RecordedBy <= 0 || value.Amount <= 0) return Fail("Payment details are invalid.");
        var invoice = (await _repository.GetInvoicesAsync()).FirstOrDefault(i => i.InvoiceID == value.InvoiceID);
        if (invoice == null) return Fail("Invoice was not found.");
        if (invoice.Status is not ("Approved" or "Partially Paid")) return Fail("Only approved invoices can be paid.");
        if (value.Amount > invoice.Balance + 0.01m) return Fail($"Payment exceeds the outstanding balance of Rs. {invoice.Balance:N2}.");
        return await Insert(() => _repository.AddPaymentAsync(value), "Payment recorded.");
    }

    private static ProcurementActionResultDto Fail(string message) => new() { Success = false, Message = message };
    private static async Task<ProcurementActionResultDto> Insert(Func<Task<int>> action, string message)
    {
        try { return new() { Success = true, Message = message, NewID = await action() }; }
        catch (Exception ex) { return Fail(ex.Message); }
    }
}

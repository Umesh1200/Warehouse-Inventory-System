using System.ComponentModel.DataAnnotations;

namespace IMS.Models.DTOs;

public class ProcurementOrderWorkflowDto
{
    public int POID { get; set; }
    public string PONumber { get; set; } = string.Empty;
    public string POStatus { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string SupplierEmail { get; set; } = string.Empty;
    public string SupplierPhone { get; set; } = string.Empty;
    public string SupplierAddress { get; set; } = string.Empty;
    public string SupplierConfirmationStatus { get; set; } = "Pending";
    public DateTime? SupplierConfirmedAt { get; set; }
    public DateTime? PromisedDeliveryDate { get; set; }
    public decimal ReceivedValue { get; set; }
    public decimal AcceptedValue { get; set; }
    public List<PurchaseOrderItemDto> Items { get; set; } = new();
    public List<POCommunicationDto> Communications { get; set; } = new();
    public List<SupplierConfirmationDto> Confirmations { get; set; } = new();
    public List<ProcurementDocumentDto> Documents { get; set; } = new();
}

public class POCommunicationDto
{
    public int CommunicationID { get; set; }
    public int POID { get; set; }
    [Required, StringLength(30)] public string Channel { get; set; } = "Email";
    [Required, StringLength(20)] public string Direction { get; set; } = "Outbound";
    [StringLength(150)] public string ContactDetail { get; set; } = string.Empty;
    public DateTime CommunicatedAt { get; set; } = DateTime.Now;
    [StringLength(1000)] public string Notes { get; set; } = string.Empty;
    public int RecordedBy { get; set; }
    public string RecordedByName { get; set; } = string.Empty;
}

public class SupplierConfirmationDto
{
    public int ConfirmationID { get; set; }
    public int POID { get; set; }
    [Required, StringLength(30)] public string ConfirmationStatus { get; set; } = "Confirmed";
    [StringLength(150)] public string SupplierContactName { get; set; } = string.Empty;
    public DateTime ConfirmedAt { get; set; } = DateTime.Now;
    public DateTime? PromisedDeliveryDate { get; set; }
    [StringLength(1000)] public string Notes { get; set; } = string.Empty;
    public int RecordedBy { get; set; }
    public string RecordedByName { get; set; } = string.Empty;
}

public class ProcurementDocumentDto
{
    public int DocumentID { get; set; }
    public int POID { get; set; }
    public int? ReceiptID { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Notes { get; set; } = string.Empty;
    public int UploadedBy { get; set; }
    public string UploadedByName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}

public class SupplierInvoiceDto
{
    public int InvoiceID { get; set; }
    public int POID { get; set; }
    public string PONumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal POAmount { get; set; }
    public decimal ReceivedValue { get; set; }
    public decimal AcceptedValue { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance => TotalAmount - PaidAmount;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string MatchStatus { get; set; } = string.Empty;
}

public class SupplierInvoiceCreateDto
{
    [Range(1, int.MaxValue)] public int POID { get; set; }
    [Required, StringLength(100)] public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; } = DateTime.Today;
    public DateTime? DueDate { get; set; }
    [Range(0, double.MaxValue)] public decimal Subtotal { get; set; }
    [Range(0, double.MaxValue)] public decimal TaxAmount { get; set; }
    [Range(0.01, double.MaxValue)] public decimal TotalAmount { get; set; }
    [StringLength(1000)] public string Notes { get; set; } = string.Empty;
    public int RecordedBy { get; set; }
}

public class SupplierPaymentCreateDto
{
    [Range(1, int.MaxValue)] public int InvoiceID { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    [Range(0.01, double.MaxValue)] public decimal Amount { get; set; }
    [Required, StringLength(40)] public string PaymentMethod { get; set; } = "Bank Transfer";
    [StringLength(100)] public string ReferenceNumber { get; set; } = string.Empty;
    [StringLength(500)] public string Notes { get; set; } = string.Empty;
    public int RecordedBy { get; set; }
}

public class ProcurementActionResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? NewID { get; set; }
}

using System;

namespace IMS.Models.DTOs
{
    public class SupplierDto
    {
        public int SupplierID { get; set; }
        public string SupplierCode { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string SupplierType { get; set; } = string.Empty;
        public int LeadTimeDays { get; set; }
        public string PaymentTerms { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ReturnPolicy { get; set; } = string.Empty;
        public string ReturnEmail { get; set; } = string.Empty;
        public string PreferredCarrier { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class SupplierSummaryDto
    {
        public int TotalSuppliers { get; set; }
        public int ActiveSuppliers { get; set; }
        public int PendingReview { get; set; }
        public int AverageLeadTime { get; set; }
    }

    public class SupplierCreateDto
    {
        public string SupplierCode { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string SupplierType { get; set; } = string.Empty;
        public int LeadTimeDays { get; set; }
        public string PaymentTerms { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public string ReturnPolicy { get; set; } = string.Empty;
        public string ReturnEmail { get; set; } = string.Empty;
        public string PreferredCarrier { get; set; } = string.Empty;
    }

    public class SupplierUpdateDto
    {
        public int SupplierID { get; set; }
        public string SupplierCode { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string SupplierType { get; set; } = string.Empty;
        public int LeadTimeDays { get; set; }
        public string PaymentTerms { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ReturnPolicy { get; set; } = string.Empty;
        public string ReturnEmail { get; set; } = string.Empty;
        public string PreferredCarrier { get; set; } = string.Empty;
    }

    public class SupplierActionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? NewID { get; set; }
    }
}

using System;
using System.Collections.Generic;

namespace IMS.Models.DTOs
{
    public class AuditLogDto
    {
        public int MovementID { get; set; }
        public DateTime MovementDate { get; set; }
        public string MovementType { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string ShelfCode { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal BeforeQty { get; set; }
        public decimal AfterQty { get; set; }
        public string ReferenceType { get; set; } = string.Empty;
        public string ReferenceNo { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;

        public string FullShelfPath => $"{WarehouseName} -> {ShelfCode}";
    }

    public class AuditLogFilterDto
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? MovementType { get; set; }
        public int? ProductID { get; set; }
        public int? PerformedByUserID { get; set; }
        public string? ReferenceNo { get; set; }
    }

    public class AuditLogSummaryDto
    {
        public int TotalMovements { get; set; }
        public decimal TotalStockIn { get; set; }
        public decimal TotalStockOut { get; set; }
        public decimal TotalAdjustments { get; set; }
        public decimal TotalTransfers { get; set; }
        public string MostActiveUser { get; set; } = "N/A";
    }

    public class DropdownOptionDto
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
}

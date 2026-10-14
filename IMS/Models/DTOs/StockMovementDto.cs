using System;

namespace IMS.Models.DTOs
{
    public class StockMovementDto
    {
        public int MovementID { get; set; }
        public DateTime MovementDate { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string ShelfCode { get; set; } = string.Empty;
        public string MovementType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal BeforeQty { get; set; }
        public decimal AfterQty { get; set; }
        public string ReferenceType { get; set; } = string.Empty;
        public string ReferenceNo { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
    }

    public class StockMovementSummaryDto
    {
        public int TotalMovements { get; set; }
        public decimal TotalStockIn { get; set; }
        public decimal TotalStockOut { get; set; }
        public int TotalTransfers { get; set; }
        public int TotalAdjustments { get; set; }
    }

    public class StockMovementTrendDto
    {
        public DateTime Date { get; set; }
        public decimal InboundQty { get; set; }
        public decimal OutboundQty { get; set; }
    }
}

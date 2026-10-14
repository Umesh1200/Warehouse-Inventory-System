using System;
using System.Collections.Generic;

namespace IMS.Models.DTOs
{
    public class DispatchTrackingDto
    {
        public int? DispatchID { get; set; } // Null for packed but not yet dispatched
        public int SOID { get; set; }
        public string SONumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public int? PackID { get; set; }
        public string CourierName { get; set; } = string.Empty;
        public string VehicleNumber { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
        public int? DispatchedBy { get; set; }
        public string DispatchedByName { get; set; } = string.Empty;
        public DateTime? DispatchDate { get; set; }
        public string Status { get; set; } = string.Empty;
        
        // Order Info
        public DateTime OrderDate { get; set; }
        public DateTime? RequiredDate { get; set; }
        public decimal TotalAmount { get; set; }

        // Aggregates
        public int ItemCount { get; set; }
        public decimal TotalOrderedQty { get; set; }
        public decimal TotalDispatchedQty { get; set; }
    }

    public class DispatchItemDto
    {
        public int SOItemID { get; set; }
        public int SOID { get; set; }
        public int ProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal OrderedQty { get; set; }
        public decimal DispatchedQty { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal ReservedQty { get; set; }
        public decimal AvailableQty { get; set; }
    }

    public class DispatchSummaryDto
    {
        public int ReadyToDispatchCount { get; set; }
        public int DispatchedCount { get; set; }
        public int InTransitCount { get; set; }
        public int DeliveredCount { get; set; }
        public int FailedCount { get; set; }
        public int TotalDispatches { get; set; }
    }

    public class DispatchCreateDto
    {
        public int SOID { get; set; }
        public int? PackID { get; set; }
        public string? CourierName { get; set; }
        public string? VehicleNumber { get; set; }
        public string? TrackingNumber { get; set; }
        public int DispatchedBy { get; set; }
    }

    public class SalesOrderForDispatchDto
    {
        public int SOID { get; set; }
        public int? PackID { get; set; }
        public string SONumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
    }

    public class DispatchActionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? NewID { get; set; }
    }
}

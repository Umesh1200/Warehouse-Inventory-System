using System;
using System.Collections.Generic;

namespace IMS.Models.DTOs
{
    public class SalesOrderDto
    {
        public int SOID { get; set; }
        public string SONumber { get; set; } = string.Empty;
        public int CustomerID { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime? RequiredDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public int CreatedBy { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;

        // Summaries from items
        public int ItemCount { get; set; }
        public decimal TotalOrderedQty { get; set; }
        public decimal TotalDispatchedQty { get; set; }
    }

    public class SalesOrderItemDto
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
    }

    public class SalesOrderSummaryDto
    {
        public int TotalOrders { get; set; }
        public int ConfirmedOrders { get; set; }
        public int PickingOrders { get; set; } // ReadyToPick, Picking
        public int DispatchPendingOrders { get; set; } // Packed, Dispatched
        public int FulfilledOrders { get; set; }
    }

    public class SalesOrderCreateDto
    {
        public string? SONumber { get; set; }
        public int CustomerID { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Today;
        public DateTime? RequiredDate { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public int CreatedBy { get; set; }
        public List<SalesOrderItemCreateDto> Items { get; set; } = new();
    }

    public class SalesOrderUpdateDto
    {
        public int SOID { get; set; }
        public string SONumber { get; set; } = string.Empty;
        public int CustomerID { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? RequiredDate { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public List<SalesOrderItemCreateDto> Items { get; set; } = new();
    }

    public class SalesOrderItemCreateDto
    {
        public int ProductID { get; set; }
        public decimal OrderedQty { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => OrderedQty * UnitPrice;
    }

    public class SalesOrderActionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? NewID { get; set; }
    }
}

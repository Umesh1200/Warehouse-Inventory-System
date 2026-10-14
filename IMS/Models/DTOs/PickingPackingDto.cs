using System;
using System.Collections.Generic;

namespace IMS.Models.DTOs
{
    public class PickingPackingDto
    {
        public int PickID { get; set; }
        public int SOID { get; set; }
        public string SONumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public int? AssignedTo { get; set; }
        public string AssignedToName { get; set; } = string.Empty;
        public string PickStatus { get; set; } = string.Empty;
        public DateTime PickCreatedAt { get; set; }
        public DateTime? PickCompletedDate { get; set; }
        
        public int? PackID { get; set; }
        public int? PackedBy { get; set; }
        public string PackedByName { get; set; } = string.Empty;
        public string ContainerType { get; set; } = string.Empty;
        public string PackStatus { get; set; } = string.Empty;
        public DateTime? PackCreatedAt { get; set; }

        public int ItemCount { get; set; }
        public decimal TotalOrderedQty { get; set; }
        public decimal TotalDispatchedQty { get; set; }
    }

    public class PickingPackingItemDto
    {
        public int SOItemID { get; set; }
        public int SOID { get; set; }
        public int ProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal OrderedQty { get; set; }
        public decimal DispatchedQty { get; set; }
        
        // Stock info
        public decimal ReservedQty { get; set; }
        public decimal AvailableQty { get; set; }
        
        // Location
        public int? ShelfID { get; set; }
        public string ShelfCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string FloorName { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string RackName { get; set; } = string.Empty;
    }

    public class PickingPackingSummaryDto
    {
        public int TotalPickTasks { get; set; }
        public int PendingPicking { get; set; }
        public int PickingInProgress { get; set; }
        public int PickedOrders { get; set; }
        public int PackedOrders { get; set; }
    }

    public class CreatePickTaskDto
    {
        public int SOID { get; set; }
        public int AssignedTo { get; set; }
        public List<PickItemReservationDto> Items { get; set; } = new();
    }

    public class PickItemReservationDto
    {
        public int ProductID { get; set; }
        public int ShelfID { get; set; }
        public decimal QtyToReserve { get; set; }
    }

    public class PackingCreateDto
    {
        public int SOID { get; set; }
        public int PackedBy { get; set; }
        public string? ContainerType { get; set; }
    }

    public class SalesOrderForPickingDto
    {
        public int SOID { get; set; }
        public string SONumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<PickingPackingItemDto> Items { get; set; } = new();
    }

    public class PickingPackingActionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? NewID { get; set; }
    }
}

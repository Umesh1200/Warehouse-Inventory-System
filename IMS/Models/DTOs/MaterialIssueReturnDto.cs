using System;

namespace IMS.Models.DTOs
{
    public class MaterialIssueReturnDto
    {
        public int IssueID { get; set; }
        public int PlanID { get; set; }
        public string PlanNumber { get; set; } = string.Empty;
        public int ProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int ShelfID { get; set; }
        public string ShelfCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string FloorName { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string RackName { get; set; } = string.Empty;
        public decimal RequestedQty { get; set; }
        public decimal IssuedQty { get; set; }
        public decimal ReturnedQty { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? IssuedBy { get; set; }
        public string IssuedByName { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public decimal AvailableQty { get; set; }
    }

    public class MaterialIssueReturnSummaryDto
    {
        public int TotalRequests { get; set; }
        public int PendingIssues { get; set; }
        public decimal TotalIssuedQty { get; set; }
        public decimal TotalReturnedQty { get; set; }
        public decimal TotalShortageQty { get; set; }
    }

    public class MaterialIssueCreateDto
    {
        public int PlanID { get; set; }
        public int ProductID { get; set; }
        public int ShelfID { get; set; }
        public decimal RequestedQty { get; set; }
        public int IssuedBy { get; set; } // RequestedBy actually but using IssuedBy context
    }

    public class MaterialIssueConfirmDto
    {
        public int IssueID { get; set; }
        public int PerformedBy { get; set; }
    }

    public class MaterialReturnDto
    {
        public int IssueID { get; set; }
        public decimal ReturnedQty { get; set; }
        public int ShelfID { get; set; }
        public int ReturnedBy { get; set; }
    }

    public class MaterialIssueReturnActionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? NewID { get; set; }
    }
}

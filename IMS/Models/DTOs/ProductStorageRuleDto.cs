using System;

namespace IMS.Models.DTOs
{
    public class ProductStorageRuleDto
    {
        public int RuleID { get; set; }
        public int ProductID { get; set; }
        public string AllowedShelfType { get; set; } = string.Empty;
        public decimal? MaxStackQty { get; set; }
        public bool RequiresQCHold { get; set; }
        public bool IsFragile { get; set; }
        public bool IsHazardous { get; set; }
        public int? PreferredWarehouseID { get; set; }
        public string PreferredWarehouseName { get; set; } = string.Empty;
        public int? PreferredRackID { get; set; }
        public string PreferredRackName { get; set; } = string.Empty;
        public int? PreferredShelfID { get; set; }
        public string PreferredShelfCode { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ProductStorageRuleSaveDto
    {
        public int RuleID { get; set; }
        public int ProductID { get; set; }
        public string AllowedShelfType { get; set; } = string.Empty;
        public decimal? MaxStackQty { get; set; }
        public bool RequiresQCHold { get; set; }
        public bool IsFragile { get; set; }
        public bool IsHazardous { get; set; }
        public int? PreferredWarehouseID { get; set; }
        public int? PreferredRackID { get; set; }
        public int? PreferredShelfID { get; set; }
    }

    public class ProductStorageRuleActionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

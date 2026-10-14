using System;
using System.Collections.Generic;

namespace IMS.Models.DTOs
{
    public class BomDto
    {
        public int BOMID { get; set; }
        public string BOMVersion { get; set; } = string.Empty;
        public int ProductID { get; set; }
        public string FinishedProductSKU { get; set; } = string.Empty;
        public string FinishedProductName { get; set; } = string.Empty;
        public DateTime? EffectiveDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int CreatedBy { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        
        // Aggregates
        public int ItemCount { get; set; }
        public decimal TotalRequiredQty { get; set; }
    }

    public class BomItemDto
    {
        public int BOMItemID { get; set; }
        public int BOMID { get; set; }
        public int RawMaterialID { get; set; }
        public string RawMaterialSKU { get; set; } = string.Empty;
        public string RawMaterialName { get; set; } = string.Empty;
        public decimal RequiredQty { get; set; }
        public decimal WastagePercent { get; set; }
        public string UnitCode { get; set; } = string.Empty;
        public string RawMaterialType { get; set; } = string.Empty;
    }

    public class BomSummaryDto
    {
        public int TotalBoms { get; set; }
        public int ActiveBoms { get; set; }
        public int DraftBoms { get; set; }
        public int UnderReviewBoms { get; set; }
        public int ArchivedBoms { get; set; }
    }

    public class BomCreateDto
    {
        public string BOMVersion { get; set; } = string.Empty;
        public int ProductID { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public string Status { get; set; } = "Draft";
        public int CreatedBy { get; set; }
        public List<BomItemCreateDto> Items { get; set; } = new();

        // For new Finished Product creation
        public bool IsNewFinishedProduct { get; set; }
        public string NewFinishedSKU { get; set; } = string.Empty;
        public string NewFinishedName { get; set; } = string.Empty;
        public int NewFinishedCategoryID { get; set; }
        public int NewFinishedUnitID { get; set; }
        public decimal NewFinishedStandardCost { get; set; }
        public string NewFinishedNotes { get; set; } = string.Empty;
    }

    public class BomItemCreateDto
    {
        // If ProductID is 0, it means it's a new raw material to be created first!
        public int ProductID { get; set; }
        
        // For new materials (if ProductID == 0)
        public string NewMaterialSKU { get; set; } = string.Empty;
        public string NewMaterialName { get; set; } = string.Empty;
        public int NewMaterialCategoryID { get; set; }
        public int NewMaterialUnitID { get; set; }
        public decimal NewMaterialStandardCost { get; set; }
        public string NewMaterialNotes { get; set; } = string.Empty; // Description/Notes
        
        public decimal RequiredQty { get; set; }
        public decimal WastagePercent { get; set; }
    }

    public class BomActionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? NewID { get; set; }
    }

    public class ComponentProcurementDto
    {
        public int ProductID { get; set; }
        public decimal? LastPOUnitPrice { get; set; }
        public string? LastPONumber { get; set; }
        public DateTime? LastPODate { get; set; }
        public string? SupplierName { get; set; }
        public DateTime? OpenPOExpectedDate { get; set; }
        public string? OpenPONumber { get; set; }
        public decimal StandardCost { get; set; }
    }
}

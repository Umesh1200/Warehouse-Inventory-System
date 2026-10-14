using IMS.Models.DTOs;
using IMS.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.Services
{
    public class BomService : IBomService
    {
        private readonly IBomRepository _bomRepository;

        public BomService(IBomRepository bomRepository)
        {
            _bomRepository = bomRepository;
        }

        public async Task<IEnumerable<BomDto>> GetBomsAsync()
        {
            return await _bomRepository.GetAllBomsAsync();
        }

        public async Task<IEnumerable<BomItemDto>> GetBomItemsAsync(int bomId)
        {
            return await _bomRepository.GetBomItemsAsync(bomId);
        }

        public async Task<BomSummaryDto> GetBomSummaryAsync()
        {
            var boms = await _bomRepository.GetAllBomsAsync();
            var bomList = boms.ToList();

            return new BomSummaryDto
            {
                TotalBoms = bomList.Count,
                ActiveBoms = bomList.Count(b => b.Status == "Active"),
                DraftBoms = bomList.Count(b => b.Status == "Draft"),
                UnderReviewBoms = bomList.Count(b => b.Status == "Under Review"),
                ArchivedBoms = bomList.Count(b => b.Status == "Archived")
            };
        }

        public async Task<BomActionResultDto> CreateBomAsync(BomCreateDto bom)
        {
            if (bom == null) return new BomActionResultDto { Success = false, Message = "BOM request is empty." };
            if (string.IsNullOrWhiteSpace(bom.BOMVersion)) return new BomActionResultDto { Success = false, Message = "BOM Version is required." };
            
            if (bom.IsNewFinishedProduct || (bom.ProductID == 0 && !string.IsNullOrWhiteSpace(bom.NewFinishedSKU)))
            {
                if (string.IsNullOrWhiteSpace(bom.NewFinishedSKU)) return new BomActionResultDto { Success = false, Message = "SKU is required for new finished product." };
                if (string.IsNullOrWhiteSpace(bom.NewFinishedName)) return new BomActionResultDto { Success = false, Message = "Name is required for new finished product." };
                if (bom.NewFinishedCategoryID <= 0) return new BomActionResultDto { Success = false, Message = "Category is required for new finished product." };
                if (bom.NewFinishedUnitID <= 0) return new BomActionResultDto { Success = false, Message = "Unit is required for new finished product." };
                if (bom.NewFinishedStandardCost < 0) return new BomActionResultDto { Success = false, Message = "Standard Cost cannot be negative for new finished product." };
            }
            else
            {
                if (bom.ProductID <= 0) return new BomActionResultDto { Success = false, Message = "Finished Product selection is required." };
            }

            if (bom.Items == null || !bom.Items.Any()) return new BomActionResultDto { Success = false, Message = "At least one BOM component item is required." };

            foreach (var item in bom.Items)
            {
                if (item.ProductID == 0)
                {
                    if (string.IsNullOrWhiteSpace(item.NewMaterialSKU)) return new BomActionResultDto { Success = false, Message = "SKU is required for new material component." };
                    if (string.IsNullOrWhiteSpace(item.NewMaterialName)) return new BomActionResultDto { Success = false, Message = "Name is required for new material component." };
                    if (item.NewMaterialCategoryID <= 0) return new BomActionResultDto { Success = false, Message = "Category selection is required for new material component." };
                    if (item.NewMaterialUnitID <= 0) return new BomActionResultDto { Success = false, Message = "Unit selection is required for new material component." };
                    if (item.NewMaterialStandardCost < 0) return new BomActionResultDto { Success = false, Message = "Standard Cost cannot be negative for new material." };
                }
                else if (item.ProductID < 0)
                {
                    return new BomActionResultDto { Success = false, Message = "Invalid Product selected in BOM items." };
                }

                if (item.RequiredQty <= 0) return new BomActionResultDto { Success = false, Message = "Required quantity must be greater than zero." };
                if (item.WastagePercent < 0 || item.WastagePercent > 100) return new BomActionResultDto { Success = false, Message = "Wastage percent must be between 0 and 100." };
            }

            try
            {
                int newId = await _bomRepository.CreateBomAsync(bom);
                return new BomActionResultDto { Success = true, Message = "Bill of Materials created successfully.", NewID = newId };
            }
            catch (Exception ex)
            {
                return new BomActionResultDto { Success = false, Message = ex.Message };
            }
        }
    }
}

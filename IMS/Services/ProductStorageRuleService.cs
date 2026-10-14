using IMS.Models.DTOs;
using IMS.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Services
{
    public class ProductStorageRuleService : IProductStorageRuleService
    {
        private readonly IProductStorageRuleRepository _ruleRepository;

        public ProductStorageRuleService(IProductStorageRuleRepository ruleRepository)
        {
            _ruleRepository = ruleRepository;
        }

        public async Task<IEnumerable<ProductStorageRuleDto>> GetRulesByProductIDAsync(int productId)
        {
            if (productId <= 0) return new List<ProductStorageRuleDto>();
            return await _ruleRepository.GetRulesByProductIDAsync(productId);
        }

        public async Task<ProductStorageRuleActionResultDto> SaveRuleAsync(ProductStorageRuleSaveDto rule)
        {
            if (rule.ProductID <= 0)
                return new ProductStorageRuleActionResultDto { Success = false, Message = "Invalid Product ID." };

            if (string.IsNullOrWhiteSpace(rule.AllowedShelfType))
                return new ProductStorageRuleActionResultDto { Success = false, Message = "Allowed Shelf Type is required." };

            if (rule.MaxStackQty.HasValue && rule.MaxStackQty.Value < 0)
                return new ProductStorageRuleActionResultDto { Success = false, Message = "Max Stack Quantity cannot be negative." };

            // Hierarchy verification
            if (rule.PreferredShelfID.HasValue && !rule.PreferredRackID.HasValue)
                return new ProductStorageRuleActionResultDto { Success = false, Message = "Preferred Rack is required when a Preferred Shelf is designated." };

            if (rule.PreferredRackID.HasValue && !rule.PreferredWarehouseID.HasValue)
                return new ProductStorageRuleActionResultDto { Success = false, Message = "Preferred Warehouse is required when a Preferred Rack is designated." };

            // Uniqueness validation (restricting one active rule per AllowedShelfType per ProductID)
            var exists = await _ruleRepository.CheckRuleExistsAsync(rule.ProductID, rule.AllowedShelfType, rule.RuleID > 0 ? rule.RuleID : (int?)null);
            if (exists)
            {
                return new ProductStorageRuleActionResultDto 
                { 
                    Success = false, 
                    Message = $"An active storage rule for shelf type '{rule.AllowedShelfType}' already exists for this product." 
                };
            }

            try
            {
                int ruleId = await _ruleRepository.SaveRuleAsync(rule);
                return new ProductStorageRuleActionResultDto 
                { 
                    Success = true, 
                    Message = rule.RuleID > 0 ? "Storage rule updated successfully." : "Storage rule created successfully." 
                };
            }
            catch (Exception ex)
            {
                return new ProductStorageRuleActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<ProductStorageRuleActionResultDto> DeactivateRuleAsync(int ruleId)
        {
            if (ruleId <= 0)
                return new ProductStorageRuleActionResultDto { Success = false, Message = "Invalid Rule ID." };

            try
            {
                var success = await _ruleRepository.DeactivateRuleAsync(ruleId);
                if (success)
                {
                    return new ProductStorageRuleActionResultDto { Success = true, Message = "Storage rule deactivated successfully." };
                }
                else
                {
                    return new ProductStorageRuleActionResultDto { Success = false, Message = "Storage rule not found or already inactive." };
                }
            }
            catch (Exception ex)
            {
                return new ProductStorageRuleActionResultDto { Success = false, Message = ex.Message };
            }
        }
    }
}

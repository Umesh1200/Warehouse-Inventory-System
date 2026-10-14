using System.Collections.Generic;
using System.Threading.Tasks;
using IMS.Models.DTOs;

namespace IMS.Services
{
    public interface IProductStorageRuleService
    {
        Task<IEnumerable<ProductStorageRuleDto>> GetRulesByProductIDAsync(int productId);
        Task<ProductStorageRuleActionResultDto> SaveRuleAsync(ProductStorageRuleSaveDto rule);
        Task<ProductStorageRuleActionResultDto> DeactivateRuleAsync(int ruleId);
    }
}

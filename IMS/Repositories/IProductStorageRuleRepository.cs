using System.Collections.Generic;
using System.Threading.Tasks;
using IMS.Models.DTOs;

namespace IMS.Repositories
{
    public interface IProductStorageRuleRepository
    {
        Task<IEnumerable<ProductStorageRuleDto>> GetRulesByProductIDAsync(int productId);
        Task<int> SaveRuleAsync(ProductStorageRuleSaveDto rule);
        Task<bool> DeactivateRuleAsync(int ruleId);
        Task<bool> CheckRuleExistsAsync(int productId, string allowedShelfType, int? excludeRuleId = null);
        Task<ProductStorageRuleDto?> GetRuleByIdAsync(int ruleId);
    }
}

using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public interface IMaterialIssueReturnRepository
    {
        Task<IEnumerable<MaterialIssueReturnDto>> GetIssueReturnTasksAsync();
        Task<int> CreateIssueRequestAsync(MaterialIssueCreateDto issue);
        Task<MaterialIssueReturnActionResultDto> IssueMaterialAsync(MaterialIssueConfirmDto issue);
        Task<MaterialIssueReturnActionResultDto> ReturnMaterialAsync(MaterialReturnDto returnDto);
        Task<MaterialIssueReturnDto?> GetIssueByIdAsync(int issueId);
        Task<decimal> GetAvailableStockAsync(int productId, int shelfId);
        Task<IEnumerable<dynamic>> GetActiveProductionPlansAsync();
        Task<IEnumerable<dynamic>> GetRawMaterialsAsync();
        Task<IEnumerable<dynamic>> GetActiveShelvesAsync();
        Task<IEnumerable<dynamic>> GetShelvesWithProductAsync(int productId);
        Task<bool> AutoGenerateMaterialIssuesAsync(int planId, int performedBy);
    }
}

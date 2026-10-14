using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public interface IPutawayRepository
    {
        Task<IEnumerable<PutawayDto>> GetPutawayTasksAsync();
        Task<PutawayActionResultDto> CreatePutawayTaskAsync(PutawayTaskCreateDto task);
        Task<PutawayActionResultDto> CompletePutawayAsync(PutawayCompleteDto putaway);
        Task<QCItemForPutawayDto?> GetQCItemForPutawayAsync(int qcId);
        Task<PutawayDto?> GetPutawayTaskByIdAsync(int taskId);
        Task<IEnumerable<ShelfOptionDto>> GetActiveShelvesAsync();
        Task<IEnumerable<ShelfOptionDto>> GetSuggestedShelvesAsync(int productId, decimal quantity);
        Task<decimal> GetShelfAvailableCapacityAsync(int shelfId);
        Task<bool> IsShelfCompatibleForProductAsync(int productId, int shelfId);
        Task<decimal> GetRequiredWeightAsync(int productId, decimal quantity);
        Task<decimal> GetShelfAvailableWeightCapacityAsync(int shelfId);
        Task<bool> HasPutawayTaskForQCAsync(int qcId);
    }
}

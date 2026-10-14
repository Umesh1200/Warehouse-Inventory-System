using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Services
{
    public interface IPutawayService
    {
        Task<IEnumerable<PutawayDto>> GetPutawayTasksAsync();
        Task<PutawaySummaryDto> GetPutawaySummaryAsync();
        Task<PutawayActionResultDto> CreatePutawayTaskAsync(PutawayTaskCreateDto task);
        Task<PutawayActionResultDto> CompletePutawayAsync(PutawayCompleteDto putaway);
        Task<IEnumerable<ShelfOptionDto>> GetActiveShelvesAsync();
        Task<IEnumerable<ShelfOptionDto>> GetSuggestedShelvesAsync(int productId, decimal quantity);
    }
}

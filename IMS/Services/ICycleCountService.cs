using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Services
{
    public interface ICycleCountService
    {
        Task<IEnumerable<CycleCountDto>> GetCycleCountsAsync();
        Task<CycleCountSummaryDto> GetCycleCountSummaryAsync();
        Task<CycleCountActionResultDto> CreateCycleCountAsync(CycleCountCreateDto request, int requestedBy);
        Task<CycleCountActionResultDto> StartCountAsync(int countId, int userId);
        Task<CycleCountActionResultDto> SubmitCountAsync(int countId, decimal countedQty, int userId);
        Task<CycleCountActionResultDto> CreateAdjustmentAsync(int countId, int userId);
    }
}

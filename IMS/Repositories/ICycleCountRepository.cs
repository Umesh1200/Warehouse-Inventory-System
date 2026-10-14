using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public interface ICycleCountRepository
    {
        Task<IEnumerable<CycleCountDto>> GetAllCycleCountsAsync();
        Task<CycleCountDto?> GetCycleCountByIdAsync(int countId);
        Task<int> CreateCycleCountAsync(CycleCountCreateDto request, int requestedBy);
        Task<bool> UpdateCycleCountStatusAsync(int countId, string status, decimal? countedQty = null);
    }
}

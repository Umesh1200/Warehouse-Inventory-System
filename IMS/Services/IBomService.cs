using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Services
{
    public interface IBomService
    {
        Task<IEnumerable<BomDto>> GetBomsAsync();
        Task<IEnumerable<BomItemDto>> GetBomItemsAsync(int bomId);
        Task<BomSummaryDto> GetBomSummaryAsync();
        Task<BomActionResultDto> CreateBomAsync(BomCreateDto bom);
    }
}

using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public interface IBomRepository
    {
        Task<IEnumerable<BomDto>> GetAllBomsAsync();
        Task<IEnumerable<BomItemDto>> GetBomItemsAsync(int bomId);
        Task<int> CreateBomAsync(BomCreateDto bom);
    }
}

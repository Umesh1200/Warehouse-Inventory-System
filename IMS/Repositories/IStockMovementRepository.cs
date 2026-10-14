using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public interface IStockMovementRepository
    {
        Task<IEnumerable<StockMovementDto>> GetRecentMovementsAsync(int limit = 100);
        Task<IEnumerable<StockMovementTrendDto>> GetMovementTrendAsync(int days = 7);
    }
}

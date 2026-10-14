using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Services
{
    public interface IStockMovementService
    {
        Task<IEnumerable<StockMovementDto>> GetRecentMovementsAsync(int limit = 100);
        Task<StockMovementSummaryDto> GetMovementSummaryAsync();
        Task<IEnumerable<StockMovementTrendDto>> GetMovementTrendAsync(int days = 7);
    }
}

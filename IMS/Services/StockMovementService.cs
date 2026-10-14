using IMS.Models.DTOs;
using IMS.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.Services
{
    public class StockMovementService : IStockMovementService
    {
        private readonly IStockMovementRepository _movementRepository;

        public StockMovementService(IStockMovementRepository movementRepository)
        {
            _movementRepository = movementRepository;
        }

        public async Task<IEnumerable<StockMovementDto>> GetRecentMovementsAsync(int limit = 100)
        {
            return await _movementRepository.GetRecentMovementsAsync(limit);
        }

        public async Task<StockMovementSummaryDto> GetMovementSummaryAsync()
        {
            var movements = await _movementRepository.GetRecentMovementsAsync(500); // Fetch a larger set for daily summary
            var moveList = movements.ToList();
            var today = DateTime.Today;

            var todayMoves = moveList.Where(m => m.MovementDate.Date == today).ToList();

            return new StockMovementSummaryDto
            {
                TotalMovements = todayMoves.Count,
                TotalStockIn = todayMoves.Where(m => new[] { "StockIn", "ReturnIn", "TransferIn", "AdjustmentIn", "ProductionReturn" }.Contains(m.MovementType)).Sum(m => m.Quantity),
                TotalStockOut = todayMoves.Where(m => new[] { "StockOut", "Dispatch", "TransferOut", "AdjustmentOut", "DamageOut", "ProductionIssue" }.Contains(m.MovementType)).Sum(m => Math.Abs(m.Quantity)),
                TotalTransfers = todayMoves.Count(m => m.MovementType.Contains("Transfer")),
                TotalAdjustments = todayMoves.Count(m => m.MovementType.Contains("Adjustment"))
            };
        }

        public async Task<IEnumerable<StockMovementTrendDto>> GetMovementTrendAsync(int days = 7)
        {
            return await _movementRepository.GetMovementTrendAsync(days);
        }
    }
}

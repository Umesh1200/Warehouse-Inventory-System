using IMS.Models.DTOs;
using IMS.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.Services
{
    public class CycleCountService : ICycleCountService
    {
        private readonly ICycleCountRepository _cycleCountRepository;
        private readonly IInventoryAdjustmentService _adjustmentService;

        public CycleCountService(ICycleCountRepository cycleCountRepository, IInventoryAdjustmentService adjustmentService)
        {
            _cycleCountRepository = cycleCountRepository;
            _adjustmentService = adjustmentService;
        }

        public async Task<IEnumerable<CycleCountDto>> GetCycleCountsAsync()
        {
            return await _cycleCountRepository.GetAllCycleCountsAsync();
        }

        public async Task<CycleCountSummaryDto> GetCycleCountSummaryAsync()
        {
            var counts = await _cycleCountRepository.GetAllCycleCountsAsync();
            var countList = counts.ToList();

            return new CycleCountSummaryDto
            {
                TotalCounts = countList.Count,
                ScheduledCount = countList.Count(c => c.Status == "Scheduled"),
                InProgressCount = countList.Count(c => c.Status == "In Progress"),
                CompletedCount = countList.Count(c => c.Status == "Completed"),
                VarianceFoundCount = countList.Count(c => c.Status == "Variance Found")
            };
        }

        public async Task<CycleCountActionResultDto> CreateCycleCountAsync(CycleCountCreateDto request, int requestedBy)
        {
            await _cycleCountRepository.CreateCycleCountAsync(request, requestedBy);
            return new CycleCountActionResultDto { Success = true, Message = "Cycle count task created successfully." };
        }

        public async Task<CycleCountActionResultDto> StartCountAsync(int countId, int userId)
        {
            await _cycleCountRepository.UpdateCycleCountStatusAsync(countId, "In Progress");
            return new CycleCountActionResultDto { Success = true, Message = "Cycle count started." };
        }

        public async Task<CycleCountActionResultDto> SubmitCountAsync(int countId, decimal countedQty, int userId)
        {
            await _cycleCountRepository.UpdateCycleCountStatusAsync(countId, "Variance Found", countedQty);
            return new CycleCountActionResultDto { Success = true, Message = "Cycle count submitted." };
        }

        public async Task<CycleCountActionResultDto> CreateAdjustmentAsync(int countId, int userId)
        {
            var count = await _cycleCountRepository.GetCycleCountByIdAsync(countId);
            if (count != null)
            {
                int productId = count.ProductID.HasValue && count.ProductID.Value > 0 ? count.ProductID.Value : 1;
                int shelfId = count.ShelfID.HasValue && count.ShelfID.Value > 0 ? count.ShelfID.Value : 1;
                decimal counted = count.CountedQty ?? 0m;
                decimal system = count.SystemQty;
                decimal diff = counted - system;

                string adjType = diff >= 0 ? "AdjustmentIn" : "AdjustmentOut";
                
                var adjDto = new AdjustmentCreateDto
                {
                    AdjustmentNo = $"ADJ-CC-{count.CountNo}",
                    AdjustmentType = adjType,
                    Reason = $"Cycle Count Variance ({count.CountNo} - {count.CountType})",
                    RequestedBy = userId > 0 ? userId : (count.AssignedTo ?? 1),
                    Items = new List<AdjustmentItemCreateDto>
                    {
                        new AdjustmentItemCreateDto
                        {
                            ProductID = productId,
                            ShelfID = shelfId,
                            SystemQty = system,
                            CountedQty = counted
                        }
                    }
                };

                await _adjustmentService.CreateAdjustmentAsync(adjDto);
            }

            await _cycleCountRepository.UpdateCycleCountStatusAsync(countId, "Completed");
            return new CycleCountActionResultDto { Success = true, Message = "Adjustment request created and sent to Adjustments page." };
        }
    }
}

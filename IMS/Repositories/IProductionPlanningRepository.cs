using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public interface IProductionPlanningRepository
    {
        Task<IEnumerable<ProductionPlanningDto>> GetAllProductionPlansAsync();
        Task<int> CreateProductionPlanAsync(ProductionPlanCreateDto plan);
        Task<bool> UpdateProductionPlanAsync(ProductionPlanUpdateDto plan);
        Task<bool> ApproveProductionPlanAsync(int planId);
        Task<bool> StartProductionPlanAsync(int planId);
        Task<bool> CancelProductionPlanAsync(int planId);
        Task<ProductionPlanningDto?> GetProductionPlanByIdAsync(int planId);
        Task<bool> IsPlanNumberExistsAsync(string planNumber);
        Task<bool> IsPlanNumberExistsForOtherPlanAsync(string planNumber, int planId);
        Task<IEnumerable<dynamic>> GetActiveBomsAsync();
        Task<IEnumerable<dynamic>> GetActiveSupervisorsAsync();
        Task<IEnumerable<dynamic>> GetMaterialShortagesAsync(int planId);
        Task<BomInspectionResultDto?> GetBomInspectionAsync(int bomId, decimal plannedQty);
    }
}

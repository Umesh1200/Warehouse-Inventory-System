using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Services
{
    public interface IProductionPlanningService
    {
        Task<IEnumerable<ProductionPlanningDto>> GetProductionPlansAsync();
        Task<ProductionPlanningSummaryDto> GetProductionPlanningSummaryAsync();
        Task<ProductionPlanningActionResultDto> CreateProductionPlanAsync(ProductionPlanCreateDto plan);
        Task<ProductionPlanningActionResultDto> UpdateProductionPlanAsync(ProductionPlanUpdateDto plan);
        Task<ProductionPlanningActionResultDto> ApproveProductionPlanAsync(int planId);
        Task<ProductionPlanningActionResultDto> StartProductionPlanAsync(int planId);
        Task<ProductionPlanningActionResultDto> CancelProductionPlanAsync(int planId);
        Task<IEnumerable<dynamic>> GetActiveBomsAsync();
        Task<IEnumerable<dynamic>> GetActiveSupervisorsAsync();
        Task<BomInspectionResultDto?> GetBomInspectionAsync(int bomId, decimal plannedQty);
    }
}

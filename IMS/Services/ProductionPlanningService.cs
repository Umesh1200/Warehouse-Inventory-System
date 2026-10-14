using IMS.Models.DTOs;
using IMS.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.Services
{
    public class ProductionPlanningService : IProductionPlanningService
    {
        private readonly IProductionPlanningRepository _repository;

        public ProductionPlanningService(IProductionPlanningRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<ProductionPlanningDto>> GetProductionPlansAsync()
        {
            return await _repository.GetAllProductionPlansAsync();
        }

        public async Task<ProductionPlanningSummaryDto> GetProductionPlanningSummaryAsync()
        {
            var plans = await _repository.GetAllProductionPlansAsync();
            var planList = plans.ToList();

            return new ProductionPlanningSummaryDto
            {
                TotalPlans = planList.Count,
                ApprovedPlans = planList.Count(p => p.Status == "Approved"),
                InProductionCount = planList.Count(p => p.Status == "In Production"),
                QCPlansCount = planList.Count(p => p.Status == "QC"),
                CompletedPlans = planList.Count(p => p.Status == "Completed"),
                PendingMaterialCheckCount = planList.Count(p => p.Status == "Material Check")
            };
        }

        public async Task<ProductionPlanningActionResultDto> CreateProductionPlanAsync(ProductionPlanCreateDto plan)
        {
            if (string.IsNullOrWhiteSpace(plan.PlanNumber))
            {
                plan.PlanNumber = $"PLN-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";
            }
            if (plan.BOMID <= 0) return new ProductionPlanningActionResultDto { Success = false, Message = "BOM is required." };
            if (plan.PlannedQty <= 0) return new ProductionPlanningActionResultDto { Success = false, Message = "Planned Quantity must be greater than zero." };
            if (plan.DueDate < plan.StartDate) return new ProductionPlanningActionResultDto { Success = false, Message = "Due Date cannot be before Start Date." };

            if (await _repository.IsPlanNumberExistsAsync(plan.PlanNumber))
            {
                return new ProductionPlanningActionResultDto { Success = false, Message = $"Plan Number '{plan.PlanNumber}' already exists." };
            }

            try
            {
                int newId = await _repository.CreateProductionPlanAsync(plan);
                return new ProductionPlanningActionResultDto { Success = true, Message = "Production Plan created successfully.", NewID = newId };
            }
            catch (Exception ex)
            {
                return new ProductionPlanningActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<ProductionPlanningActionResultDto> UpdateProductionPlanAsync(ProductionPlanUpdateDto plan)
        {
            if (plan.PlanID <= 0) return new ProductionPlanningActionResultDto { Success = false, Message = "Invalid Plan ID." };
            if (string.IsNullOrWhiteSpace(plan.PlanNumber)) return new ProductionPlanningActionResultDto { Success = false, Message = "Plan Number is required." };
            if (plan.BOMID <= 0) return new ProductionPlanningActionResultDto { Success = false, Message = "BOM is required." };
            if (plan.PlannedQty <= 0) return new ProductionPlanningActionResultDto { Success = false, Message = "Planned Quantity must be greater than zero." };

            var existing = await _repository.GetProductionPlanByIdAsync(plan.PlanID);
            if (existing == null) return new ProductionPlanningActionResultDto { Success = false, Message = "Plan not found." };
            if (existing.Status != "Draft" && existing.Status != "Material Check") 
                return new ProductionPlanningActionResultDto { Success = false, Message = "Only Draft or Material Check plans can be edited." };

            if (await _repository.IsPlanNumberExistsForOtherPlanAsync(plan.PlanNumber, plan.PlanID))
            {
                return new ProductionPlanningActionResultDto { Success = false, Message = $"Plan Number '{plan.PlanNumber}' already exists." };
            }

            try
            {
                bool success = await _repository.UpdateProductionPlanAsync(plan);
                if (success) return new ProductionPlanningActionResultDto { Success = true, Message = "Production Plan updated successfully." };
                return new ProductionPlanningActionResultDto { Success = false, Message = "Failed to update plan." };
            }
            catch (Exception ex)
            {
                return new ProductionPlanningActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<ProductionPlanningActionResultDto> ApproveProductionPlanAsync(int planId)
        {
            var existing = await _repository.GetProductionPlanByIdAsync(planId);
            if (existing == null) return new ProductionPlanningActionResultDto { Success = false, Message = "Plan not found." };
            if (existing.Status != "Draft" && existing.Status != "Material Check") 
                return new ProductionPlanningActionResultDto { Success = false, Message = "Only Draft or Material Check plans can be approved." };

            // Check shortages
            var shortages = await _repository.GetMaterialShortagesAsync(planId);
            if (shortages.Any())
            {
                return new ProductionPlanningActionResultDto { Success = false, Message = "Insufficient material stock. Plan remains in 'Material Check' status." };
            }

            try
            {
                bool success = await _repository.ApproveProductionPlanAsync(planId);
                if (success) return new ProductionPlanningActionResultDto { Success = true, Message = "Production Plan approved." };
                return new ProductionPlanningActionResultDto { Success = false, Message = "Failed to approve plan." };
            }
            catch (Exception ex)
            {
                return new ProductionPlanningActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<ProductionPlanningActionResultDto> StartProductionPlanAsync(int planId)
        {
            var existing = await _repository.GetProductionPlanByIdAsync(planId);
            if (existing == null) return new ProductionPlanningActionResultDto { Success = false, Message = "Plan not found." };
            if (existing.Status != "Approved") return new ProductionPlanningActionResultDto { Success = false, Message = "Only Approved plans can be started." };

            try
            {
                bool success = await _repository.StartProductionPlanAsync(planId);
                if (success) return new ProductionPlanningActionResultDto { Success = true, Message = "Production started." };
                return new ProductionPlanningActionResultDto { Success = false, Message = "Failed to start production." };
            }
            catch (Exception ex)
            {
                return new ProductionPlanningActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<ProductionPlanningActionResultDto> CancelProductionPlanAsync(int planId)
        {
            var existing = await _repository.GetProductionPlanByIdAsync(planId);
            if (existing == null) return new ProductionPlanningActionResultDto { Success = false, Message = "Plan not found." };
            if (new[] { "Completed", "Cancelled" }.Contains(existing.Status)) 
                return new ProductionPlanningActionResultDto { Success = false, Message = "Cannot cancel finished or already cancelled plans." };

            try
            {
                bool success = await _repository.CancelProductionPlanAsync(planId);
                if (success) return new ProductionPlanningActionResultDto { Success = true, Message = "Production Plan cancelled." };
                return new ProductionPlanningActionResultDto { Success = false, Message = "Failed to cancel plan." };
            }
            catch (Exception ex)
            {
                return new ProductionPlanningActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<IEnumerable<dynamic>> GetActiveBomsAsync()
        {
            return await _repository.GetActiveBomsAsync();
        }

        public async Task<IEnumerable<dynamic>> GetActiveSupervisorsAsync()
        {
            return await _repository.GetActiveSupervisorsAsync();
        }

        public async Task<BomInspectionResultDto?> GetBomInspectionAsync(int bomId, decimal plannedQty)
        {
            if (bomId <= 0) return null;
            if (plannedQty <= 0) plannedQty = 1;
            return await _repository.GetBomInspectionAsync(bomId, plannedQty);
        }
    }
}

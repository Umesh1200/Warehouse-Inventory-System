using IMS.Models.DTOs;
using IMS.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.Services
{
    public class MaterialIssueReturnService : IMaterialIssueReturnService
    {
        private readonly IMaterialIssueReturnRepository _repository;

        public MaterialIssueReturnService(IMaterialIssueReturnRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<MaterialIssueReturnDto>> GetIssueReturnTasksAsync()
        {
            return await _repository.GetIssueReturnTasksAsync();
        }

        public async Task<MaterialIssueReturnSummaryDto> GetIssueReturnSummaryAsync()
        {
            var tasks = await _repository.GetIssueReturnTasksAsync();
            var taskList = tasks.ToList();

            return new MaterialIssueReturnSummaryDto
            {
                TotalRequests = taskList.Count,
                PendingIssues = taskList.Count(t => t.Status == "Pending" || t.Status == "Partially Issued"),
                TotalIssuedQty = taskList.Sum(t => t.IssuedQty),
                TotalReturnedQty = taskList.Sum(t => t.ReturnedQty),
                TotalShortageQty = taskList.Sum(t => Math.Max(0, t.RequestedQty - t.AvailableQty))
            };
        }

        public async Task<MaterialIssueReturnActionResultDto> CreateIssueRequestAsync(MaterialIssueCreateDto issue)
        {
            if (issue.PlanID <= 0) return new MaterialIssueReturnActionResultDto { Success = false, Message = "Invalid Production Plan." };
            if (issue.ProductID <= 0) return new MaterialIssueReturnActionResultDto { Success = false, Message = "Invalid Product." };
            if (issue.ShelfID <= 0) return new MaterialIssueReturnActionResultDto { Success = false, Message = "Invalid Shelf." };
            if (issue.RequestedQty <= 0) return new MaterialIssueReturnActionResultDto { Success = false, Message = "Requested quantity must be greater than zero." };

            try
            {
                int newId = await _repository.CreateIssueRequestAsync(issue);
                return new MaterialIssueReturnActionResultDto { Success = true, Message = "Material issue request created.", NewID = newId };
            }
            catch (Exception ex)
            {
                return new MaterialIssueReturnActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<MaterialIssueReturnActionResultDto> IssueMaterialAsync(MaterialIssueConfirmDto issue)
        {
            if (issue.IssueID <= 0) return new MaterialIssueReturnActionResultDto { Success = false, Message = "Invalid Issue ID." };
            
            var existing = await _repository.GetIssueByIdAsync(issue.IssueID);
            if (existing == null) return new MaterialIssueReturnActionResultDto { Success = false, Message = "Issue record not found." };
            if (existing.Status == "Issued" || existing.Status == "Fulfilled") return new MaterialIssueReturnActionResultDto { Success = false, Message = "Material already issued." };

            var available = await _repository.GetAvailableStockAsync(existing.ProductID, existing.ShelfID);
            if (available < existing.RequestedQty)
            {
                return new MaterialIssueReturnActionResultDto { Success = false, Message = $"Insufficient available stock. Available: {available}, Requested: {existing.RequestedQty}." };
            }

            return await _repository.IssueMaterialAsync(issue);
        }

        public async Task<MaterialIssueReturnActionResultDto> ReturnMaterialAsync(MaterialReturnDto returnDto)
        {
            if (returnDto.IssueID <= 0) return new MaterialIssueReturnActionResultDto { Success = false, Message = "Invalid Issue ID." };
            if (returnDto.ReturnedQty <= 0) return new MaterialIssueReturnActionResultDto { Success = false, Message = "Returned quantity must be greater than zero." };
            if (returnDto.ShelfID <= 0) return new MaterialIssueReturnActionResultDto { Success = false, Message = "Select a shelf to return to." };

            var existing = await _repository.GetIssueByIdAsync(returnDto.IssueID);
            if (existing == null) return new MaterialIssueReturnActionResultDto { Success = false, Message = "Issue record not found." };
            
            decimal maxReturnable = existing.IssuedQty - existing.ReturnedQty;
            if (returnDto.ReturnedQty > maxReturnable)
            {
                return new MaterialIssueReturnActionResultDto { Success = false, Message = $"Cannot return more than issued. Max returnable: {maxReturnable}." };
            }

            return await _repository.ReturnMaterialAsync(returnDto);
        }

        public async Task<decimal> GetAvailableStockAsync(int productId, int shelfId)
        {
            return await _repository.GetAvailableStockAsync(productId, shelfId);
        }

        public async Task<IEnumerable<dynamic>> GetActiveProductionPlansAsync()
        {
            return await _repository.GetActiveProductionPlansAsync();
        }

        public async Task<IEnumerable<dynamic>> GetRawMaterialsAsync()
        {
            return await _repository.GetRawMaterialsAsync();
        }

        public async Task<IEnumerable<dynamic>> GetActiveShelvesAsync()
        {
            return await _repository.GetActiveShelvesAsync();
        }

        public async Task<IEnumerable<dynamic>> GetShelvesWithProductAsync(int productId)
        {
            return await _repository.GetShelvesWithProductAsync(productId);
        }

        public async Task<bool> AutoGenerateMaterialIssuesAsync(int planId, int performedBy)
        {
            return await _repository.AutoGenerateMaterialIssuesAsync(planId, performedBy);
        }
    }
}

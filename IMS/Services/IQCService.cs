using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Services
{
    public interface IQCService
    {
        Task<IEnumerable<QCCheckDto>> GetQCTasksAsync();
        Task<QCCheckSummaryDto> GetQCSummaryAsync();
        Task<QCActionResultDto> SubmitQCResultAsync(QCSubmitDto qc);
    }
}

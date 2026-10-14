using IMS.Models.DTOs;
using System.Threading.Tasks;

using System.Collections.Generic;

namespace IMS.Repositories
{
    public interface IReportRepository
    {
        Task<ReportDashboardDto> GetReportDashboardDataAsync();
        Task<ReportTableDataDto> GetReportDataByKeyAsync(string reportKey);
        Task<IEnumerable<ReportScheduleDto>> GetReportSchedulesAsync();
    }
}

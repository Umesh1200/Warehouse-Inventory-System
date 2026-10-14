using IMS.Models.DTOs;
using System.Threading.Tasks;

using System.Collections.Generic;

namespace IMS.Services
{
    public interface IReportService
    {
        Task<ReportDashboardDto> GetReportDashboardDataAsync();
        Task<ReportTableDataDto> GetReportDataByKeyAsync(string reportKey);
        Task<IEnumerable<ReportScheduleDto>> GetReportSchedulesAsync();
    }
}

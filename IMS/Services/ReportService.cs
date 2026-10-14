using IMS.Models.DTOs;
using IMS.Repositories;
using System.Threading.Tasks;

namespace IMS.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;

        public ReportService(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        public async Task<ReportDashboardDto> GetReportDashboardDataAsync()
        {
            return await _reportRepository.GetReportDashboardDataAsync();
        }

        public async Task<ReportTableDataDto> GetReportDataByKeyAsync(string reportKey)
        {
            return await _reportRepository.GetReportDataByKeyAsync(reportKey);
        }

        public async Task<IEnumerable<ReportScheduleDto>> GetReportSchedulesAsync()
        {
            return await _reportRepository.GetReportSchedulesAsync();
        }
    }
}

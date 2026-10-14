using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public interface ISystemSettingRepository
    {
        Task<IReadOnlyList<SystemSettingDto>> GetSettingsAsync();
        Task<SystemSettingActionResultDto> UpdateSettingsAsync(IReadOnlyList<SystemSettingUpdateDto> settings, int updatedBy);
    }
}

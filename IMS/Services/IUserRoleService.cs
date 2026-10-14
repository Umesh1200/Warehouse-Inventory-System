using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Services
{
    public interface IUserRoleService
    {
        Task<UserManagementDataDto> GetUserManagementDataAsync();
        Task<UserRoleActionResultDto> CreateUserAsync(UserCreateDto user);
        Task<UserRoleActionResultDto> UpdateUserAsync(UserUpdateDto user);
        Task<UserRoleActionResultDto> DeactivateUserAsync(int userId);
        Task<UserRoleActionResultDto> ReactivateUserAsync(int userId);
        Task<UserDto?> AuthenticateAsync(string usernameOrEmail, string password);
        Task<UserRoleActionResultDto> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    }
}

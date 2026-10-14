using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public interface IUserRoleRepository
    {
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<IEnumerable<RoleDto>> GetAllRolesAsync();
        Task<IEnumerable<RoleDto>> GetActiveRolesAsync();
        Task<IEnumerable<WarehouseDto>> GetActiveWarehousesAsync();
        Task<IEnumerable<PermissionDto>> GetAllPermissionsAsync();
        Task<UserRoleActionResultDto> CreateUserAsync(UserCreateDto user);
        Task<UserRoleActionResultDto> UpdateUserAsync(UserUpdateDto user);
        Task<UserRoleActionResultDto> DeactivateUserAsync(int userId);
        Task<UserRoleActionResultDto> ReactivateUserAsync(int userId);
        Task<UserDto?> GetUserByIdAsync(int userId);
        Task<bool> IsEmailExistsAsync(string email);
        Task<bool> IsEmailExistsForOtherUserAsync(string email, int userId);
        Task<bool> IsUsernameExistsAsync(string username);
        Task<UserDto?> GetUserByCredentialsAsync(string usernameOrEmail);
        Task<bool> ChangePasswordAsync(int userId, string newPassword);
    }
}

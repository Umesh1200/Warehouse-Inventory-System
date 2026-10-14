using IMS.Models.DTOs;
using IMS.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.Services
{
    public class UserRoleService : IUserRoleService
    {
        private readonly IUserRoleRepository _userRoleRepository;

        public UserRoleService(IUserRoleRepository userRoleRepository)
        {
            _userRoleRepository = userRoleRepository;
        }

        public async Task<UserManagementDataDto> GetUserManagementDataAsync()
        {
            var data = new UserManagementDataDto();

            try
            {
                data.Users = (await _userRoleRepository.GetAllUsersAsync()).ToList();
                data.Roles = (await _userRoleRepository.GetAllRolesAsync()).ToList();
                data.Permissions = (await _userRoleRepository.GetAllPermissionsAsync()).ToList();
                data.Warehouses = (await _userRoleRepository.GetActiveWarehousesAsync()).ToList();

                data.Summary = new UserRoleSummaryDto
                {
                    TotalUsers = data.Users.Count,
                    ActiveUsers = data.Users.Count(u => u.IsActive),
                    InactiveUsers = data.Users.Count(u => !u.IsActive),
                    TotalRoles = data.Roles.Count,
                    TotalPermissions = data.Permissions.Count
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Error compiling user management data.", ex);
            }

            return data;
        }

        public async Task<UserRoleActionResultDto> CreateUserAsync(UserCreateDto user)
        {
            if (string.IsNullOrWhiteSpace(user.Username)) return new UserRoleActionResultDto { Success = false, Message = "Username is required." };
            if (string.IsNullOrWhiteSpace(user.FullName)) return new UserRoleActionResultDto { Success = false, Message = "Full Name is required." };
            if (string.IsNullOrWhiteSpace(user.Email)) return new UserRoleActionResultDto { Success = false, Message = "Email is required." };
            if (user.RoleID <= 0) return new UserRoleActionResultDto { Success = false, Message = "Role is required." };

            if (!user.Email.Contains("@")) return new UserRoleActionResultDto { Success = false, Message = "Invalid email format." };

            if (await _userRoleRepository.IsUsernameExistsAsync(user.Username))
                return new UserRoleActionResultDto { Success = false, Message = "Username already exists." };

            if (await _userRoleRepository.IsEmailExistsAsync(user.Email))
                return new UserRoleActionResultDto { Success = false, Message = "Email already exists." };

            try
            {
                return await _userRoleRepository.CreateUserAsync(user);
            }
            catch (Exception ex)
            {
                return new UserRoleActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<UserRoleActionResultDto> UpdateUserAsync(UserUpdateDto user)
        {
            if (user.UserID <= 0) return new UserRoleActionResultDto { Success = false, Message = "Invalid User ID." };
            if (string.IsNullOrWhiteSpace(user.FullName)) return new UserRoleActionResultDto { Success = false, Message = "Full Name is required." };
            if (string.IsNullOrWhiteSpace(user.Email)) return new UserRoleActionResultDto { Success = false, Message = "Email is required." };
            if (user.RoleID <= 0) return new UserRoleActionResultDto { Success = false, Message = "Role is required." };

            if (!user.Email.Contains("@")) return new UserRoleActionResultDto { Success = false, Message = "Invalid email format." };

            if (await _userRoleRepository.IsEmailExistsForOtherUserAsync(user.Email, user.UserID))
                return new UserRoleActionResultDto { Success = false, Message = "Email already in use by another user." };

            try
            {
                return await _userRoleRepository.UpdateUserAsync(user);
            }
            catch (Exception ex)
            {
                return new UserRoleActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<UserRoleActionResultDto> DeactivateUserAsync(int userId)
        {
            if (userId <= 0) return new UserRoleActionResultDto { Success = false, Message = "Invalid User ID." };

            try
            {
                return await _userRoleRepository.DeactivateUserAsync(userId);
            }
            catch (Exception ex)
            {
                return new UserRoleActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<UserRoleActionResultDto> ReactivateUserAsync(int userId)
        {
            if (userId <= 0) return new UserRoleActionResultDto { Success = false, Message = "Invalid User ID." };

            try
            {
                return await _userRoleRepository.ReactivateUserAsync(userId);
            }
            catch (Exception ex)
            {
                return new UserRoleActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<UserDto?> AuthenticateAsync(string usernameOrEmail, string password)
        {
            var user = await _userRoleRepository.GetUserByCredentialsAsync(usernameOrEmail);
            if (user == null) return null;

            // Simple demo logic: 
            // 1. If DB has "HASHED_PWD_HERE", allow "IMS@123"
            // 2. If user was created with UI, it currently stores plain text "IMS@123" 
            // 3. In a real app, we use BCrypt/Identity
            
            bool isValid = false;
            if (user.PasswordHash == "HASHED_PWD_HERE" && password == "IMS@123")
            {
                isValid = true;
            }
            else if (user.PasswordHash == password)
            {
                isValid = true;
            }

            return isValid ? user : null;
        }

        public async Task<UserRoleActionResultDto> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _userRoleRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return new UserRoleActionResultDto { Success = false, Message = "User not found." };
            }
            
            // Check current password
            bool isValid = false;
            if (user.PasswordHash == "HASHED_PWD_HERE" && currentPassword == "IMS@123")
            {
                isValid = true;
            }
            else if (user.PasswordHash == currentPassword)
            {
                isValid = true;
            }

            if (!isValid)
            {
                return new UserRoleActionResultDto { Success = false, Message = "Incorrect current password." };
            }

            var success = await _userRoleRepository.ChangePasswordAsync(userId, newPassword);
            if (success)
            {
                return new UserRoleActionResultDto { Success = true, Message = "Password updated successfully." };
            }
            return new UserRoleActionResultDto { Success = false, Message = "Failed to update password." };
        }
    }
}

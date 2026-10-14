using IMS.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public class UserRoleRepository : IUserRoleRepository
    {
        private readonly string _connectionString;

        public UserRoleRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var results = new List<UserDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT u.UserID, u.Username, u.FullName, u.Email, u.Phone, u.RoleID, u.IsActive, u.CreatedAt, u.UpdatedAt,
                           u.SupplierID, r.RoleName
                    FROM Users u
                    LEFT JOIN Roles r ON u.RoleID = r.RoleID
                    ORDER BY u.FullName", connection);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new UserDto
                    {
                        UserID = reader.GetInt32("UserID"),
                        Username = reader.GetString("Username"),
                        FullName = reader.GetString("FullName"),
                        Email = reader.IsDBNull("Email") ? "" : reader.GetString("Email"),
                        Phone = reader.IsDBNull("Phone") ? "" : reader.GetString("Phone"),
                        RoleID = reader.IsDBNull("RoleID") ? (int?)null : reader.GetInt32("RoleID"),
                        RoleName = reader.IsDBNull("RoleName") ? "No Role" : reader.GetString("RoleName"),
                        IsActive = reader.GetBoolean("IsActive"),
                        SupplierID = reader.IsDBNull("SupplierID") ? (int?)null : reader.GetInt32("SupplierID"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        UpdatedAt = reader.GetDateTime("UpdatedAt")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching users.", ex);
            }

            return results;
        }

        public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
        {
            var results = new List<RoleDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT r.*, 
                           (SELECT COUNT(*) FROM Users u WHERE u.RoleID = r.RoleID) AS UserCount,
                           (SELECT COUNT(*) FROM RolePermissions rp WHERE rp.RoleID = r.RoleID) AS PermissionCount
                    FROM Roles r
                    ORDER BY r.RoleName", connection);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new RoleDto
                    {
                        RoleID = reader.GetInt32("RoleID"),
                        RoleName = reader.GetString("RoleName"),
                        Description = reader.IsDBNull("Description") ? "" : reader.GetString("Description"),
                        IsActive = reader.GetBoolean("IsActive"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        UserCount = reader.GetInt32("UserCount"),
                        PermissionCount = reader.GetInt32("PermissionCount")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching roles.", ex);
            }

            return results;
        }

        public async Task<IEnumerable<RoleDto>> GetActiveRolesAsync()
        {
            var results = new List<RoleDto>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand("SELECT * FROM Roles WHERE IsActive = 1 ORDER BY RoleName", connection);
                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(new RoleDto
                    {
                        RoleID = reader.GetInt32("RoleID"),
                        RoleName = reader.GetString("RoleName")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching active roles.", ex);
            }
            return results;
        }

        public async Task<IEnumerable<WarehouseDto>> GetActiveWarehousesAsync()
        {
            var results = new List<WarehouseDto>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand("SELECT WarehouseID, WarehouseName FROM Warehouses WHERE IsActive = 1 ORDER BY WarehouseName", connection);
                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(new WarehouseDto
                    {
                        WarehouseID = reader.GetInt32("WarehouseID"),
                        WarehouseName = reader.GetString("WarehouseName")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching active warehouses.", ex);
            }
            return results;
        }

        public async Task<IEnumerable<PermissionDto>> GetAllPermissionsAsync()
        {
            var results = new List<PermissionDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand("SELECT * FROM Permissions ORDER BY PermissionName", connection);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new PermissionDto
                    {
                        PermissionID = reader.GetInt32("PermissionID"),
                        PermissionName = reader.GetString("PermissionName"),
                        Description = reader.IsDBNull("Description") ? "" : reader.GetString("Description")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching permissions.", ex);
            }

            return results;
        }

        public async Task<UserRoleActionResultDto> CreateUserAsync(UserCreateDto user)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    INSERT INTO Users (Username, FullName, Email, Phone, RoleID, PasswordHash, SupplierID, IsActive, CreatedAt, UpdatedAt)
                    VALUES (@Username, @FullName, @Email, @Phone, @RoleID, @Password, @SupplierID, 1, GETDATE(), GETDATE());
                    SELECT SCOPE_IDENTITY();", connection);

                command.Parameters.AddWithValue("@Username", user.Username);
                command.Parameters.AddWithValue("@FullName", user.FullName);
                command.Parameters.AddWithValue("@Email", user.Email);
                command.Parameters.AddWithValue("@Phone", user.Phone ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@RoleID", user.RoleID);
                command.Parameters.AddWithValue("@Password", user.Password); 
                command.Parameters.AddWithValue("@SupplierID", user.SupplierID.HasValue ? (object)user.SupplierID.Value : DBNull.Value);

                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                int newId = Convert.ToInt32(result);
                return new UserRoleActionResultDto { Success = true, Message = "User created successfully.", NewID = newId };
            }
            catch (Exception ex)
            {
                return new UserRoleActionResultDto { Success = false, Message = "Error creating user: " + ex.Message };
            }
        }

        public async Task<UserRoleActionResultDto> UpdateUserAsync(UserUpdateDto user)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    UPDATE Users SET 
                        FullName = @FullName, 
                        Email = @Email, 
                        Phone = @Phone, 
                        RoleID = @RoleID, 
                        IsActive = @IsActive,
                        SupplierID = @SupplierID,
                        UpdatedAt = GETDATE()
                    WHERE UserID = @UserID", connection);

                command.Parameters.AddWithValue("@UserID", user.UserID);
                command.Parameters.AddWithValue("@FullName", user.FullName);
                command.Parameters.AddWithValue("@Email", user.Email);
                command.Parameters.AddWithValue("@Phone", user.Phone ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@RoleID", user.RoleID);
                command.Parameters.AddWithValue("@IsActive", user.IsActive);
                command.Parameters.AddWithValue("@SupplierID", user.SupplierID.HasValue ? (object)user.SupplierID.Value : DBNull.Value);

                await connection.OpenAsync();
                int rows = await command.ExecuteNonQueryAsync();
                if (rows > 0)
                    return new UserRoleActionResultDto { Success = true, Message = "User profile updated." };
                else
                    return new UserRoleActionResultDto { Success = false, Message = "User not found." };
            }
            catch (Exception ex)
            {
                return new UserRoleActionResultDto { Success = false, Message = "Error updating user: " + ex.Message };
            }
        }

        public async Task<UserRoleActionResultDto> DeactivateUserAsync(int userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    UPDATE Users SET IsActive = 0, UpdatedAt = GETDATE() WHERE UserID = @UserID", connection);
                command.Parameters.AddWithValue("@UserID", userId);
                await connection.OpenAsync();
                int rows = await command.ExecuteNonQueryAsync();
                if (rows > 0)
                    return new UserRoleActionResultDto { Success = true, Message = "User deactivated successfully. History is preserved." };
                else
                    return new UserRoleActionResultDto { Success = false, Message = "User not found." };
            }
            catch (Exception ex)
            {
                return new UserRoleActionResultDto { Success = false, Message = "Error deactivating user: " + ex.Message };
            }
        }

        public async Task<UserRoleActionResultDto> ReactivateUserAsync(int userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    UPDATE Users SET IsActive = 1, UpdatedAt = GETDATE() WHERE UserID = @UserID", connection);
                command.Parameters.AddWithValue("@UserID", userId);
                await connection.OpenAsync();
                int rows = await command.ExecuteNonQueryAsync();
                if (rows > 0)
                    return new UserRoleActionResultDto { Success = true, Message = "User account reactivated." };
                else
                    return new UserRoleActionResultDto { Success = false, Message = "User not found." };
            }
            catch (Exception ex)
            {
                return new UserRoleActionResultDto { Success = false, Message = "Error reactivating user: " + ex.Message };
            }
        }

        public async Task<UserDto?> GetUserByIdAsync(int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT u.*, r.RoleName 
                FROM Users u 
                LEFT JOIN Roles r ON u.RoleID = r.RoleID 
                WHERE u.UserID = @UserID", connection);
            command.Parameters.AddWithValue("@UserID", userId);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new UserDto
                {
                    UserID = reader.GetInt32("UserID"),
                    Username = reader.GetString("Username"),
                    FullName = reader.GetString("FullName"),
                    Email = reader.IsDBNull("Email") ? "" : reader.GetString("Email"),
                    Phone = reader.IsDBNull("Phone") ? "" : reader.GetString("Phone"),
                    RoleID = reader.IsDBNull("RoleID") ? (int?)null : reader.GetInt32("RoleID"),
                    RoleName = reader.IsDBNull("RoleName") ? "" : reader.GetString("RoleName"),
                    IsActive = reader.GetBoolean("IsActive"),
                    SupplierID = reader.IsDBNull("SupplierID") ? (int?)null : reader.GetInt32("SupplierID"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    UpdatedAt = reader.GetDateTime("UpdatedAt")
                };
            }
            return null;
        }

        public async Task<bool> IsEmailExistsAsync(string email)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT COUNT(*) FROM Users WHERE Email = @Email", connection);
            command.Parameters.AddWithValue("@Email", email);
            await connection.OpenAsync();
            var count = (int)(await command.ExecuteScalarAsync() ?? 0);
            return count > 0;
        }

        public async Task<bool> IsEmailExistsForOtherUserAsync(string email, int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT COUNT(*) FROM Users WHERE Email = @Email AND UserID != @UserID", connection);
            command.Parameters.AddWithValue("@Email", email);
            command.Parameters.AddWithValue("@UserID", userId);
            await connection.OpenAsync();
            var count = (int)(await command.ExecuteScalarAsync() ?? 0);
            return count > 0;
        }

        public async Task<bool> IsUsernameExistsAsync(string username)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT COUNT(*) FROM Users WHERE Username = @Username", connection);
            command.Parameters.AddWithValue("@Username", username);
            await connection.OpenAsync();
            var count = (int)(await command.ExecuteScalarAsync() ?? 0);
            return count > 0;
        }

        public async Task<UserDto?> GetUserByCredentialsAsync(string usernameOrEmail)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT u.*, r.RoleName 
                FROM Users u 
                LEFT JOIN Roles r ON u.RoleID = r.RoleID 
                WHERE (u.Username = @Login OR u.Email = @Login) AND u.IsActive = 1", connection);
            
            command.Parameters.AddWithValue("@Login", usernameOrEmail);
            
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new UserDto
                {
                    UserID = reader.GetInt32("UserID"),
                    Username = reader.GetString("Username"),
                    FullName = reader.GetString("FullName"),
                    Email = reader.IsDBNull("Email") ? "" : reader.GetString("Email"),
                    RoleID = reader.IsDBNull("RoleID") ? (int?)null : reader.GetInt32("RoleID"),
                    RoleName = reader.IsDBNull("RoleName") ? "" : reader.GetString("RoleName"),
                    PasswordHash = reader.GetString("PasswordHash"),
                    IsActive = reader.GetBoolean("IsActive"),
                    SupplierID = reader.IsDBNull("SupplierID") ? (int?)null : reader.GetInt32("SupplierID")
                };
            }
            return null;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string newPassword)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand("UPDATE Users SET PasswordHash = @PasswordHash, UpdatedAt = GETDATE() WHERE UserID = @UserID", connection);
                command.Parameters.AddWithValue("@PasswordHash", newPassword);
                command.Parameters.AddWithValue("@UserID", userId);
                
                await connection.OpenAsync();
                int rows = await command.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}

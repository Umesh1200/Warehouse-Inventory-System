using System;
using System.Collections.Generic;

namespace IMS.Models.DTOs
{
    public class UserDto
    {
        public int UserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int? RoleID { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int? SupplierID { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class RoleDto
    {
        public int RoleID { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int PermissionCount { get; set; }
        public int UserCount { get; set; }
    }

    public class PermissionDto
    {
        public int PermissionID { get; set; }
        public string PermissionName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class UserRoleSummaryDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int TotalRoles { get; set; }
        public int TotalPermissions { get; set; }
    }

    public class UserManagementDataDto
    {
        public List<UserDto> Users { get; set; } = new();
        public List<RoleDto> Roles { get; set; } = new();
        public List<PermissionDto> Permissions { get; set; } = new();
        public List<WarehouseDto> Warehouses { get; set; } = new();
        public UserRoleSummaryDto Summary { get; set; } = new();
    }

    public class UserCreateDto
    {
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int RoleID { get; set; }
        public string Password { get; set; } = "IMS@123"; // Default initial password
        public int? SupplierID { get; set; }
    }

    public class UserUpdateDto
    {
        public int UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int RoleID { get; set; }
        public bool IsActive { get; set; }
        public int? SupplierID { get; set; }
    }

    public class UserRoleActionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? NewID { get; set; }
    }

    public class WarehouseDto
    {
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
    }
}

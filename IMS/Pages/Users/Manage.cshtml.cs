using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using IMS.Services;
using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace IMS.Pages.Users
{
    public class ManageModel : PageModel
    {
        private readonly IUserRoleService _userRoleService;
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public ManageModel(IUserRoleService userRoleService)
        {
            _userRoleService = userRoleService;
        }

        public List<UserSummaryCard> SummaryCards { get; set; } = new();
        public List<UserRow> Users { get; set; } = new();
        
        public List<RoleDto> RolesList { get; set; } = new();
        public List<WarehouseDto> WarehouseList { get; set; } = new();
        public List<string> Roles { get; set; } = new();
        public List<string> Warehouses { get; set; } = new();

        [BindProperty]
        public UserCreateDto NewUser { get; set; } = new();

        [BindProperty]
        public UserUpdateDto EditUser { get; set; } = new();

        [BindProperty]
        public int DeactivateUserID { get; set; }

        [BindProperty]
        public int ReactivateUserID { get; set; }

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return Page();
            }

            var result = await _userRoleService.CreateUserAsync(NewUser);
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage();
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadDataAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return Page();
            }

            var result = await _userRoleService.UpdateUserAsync(EditUser);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage();
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadDataAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeactivateAsync()
        {
            var result = await _userRoleService.DeactivateUserAsync(DeactivateUserID);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage();
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadDataAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostReactivateAsync()
        {
            var result = await _userRoleService.ReactivateUserAsync(ReactivateUserID);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage();
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadDataAsync();
                return Page();
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                if (TempData["SuccessMessage"] != null)
                {
                    SuccessMessage = TempData["SuccessMessage"]?.ToString() ?? "";
                }

                // Fetch live data from SQL
                var data = await _userRoleService.GetUserManagementDataAsync();

                RolesList = data.Roles;
                WarehouseList = data.Warehouses;

                Roles = new List<string> { "All Roles" };
                Roles.AddRange(data.Roles.Select(r => r.RoleName));

                Warehouses = new List<string> { "All Warehouses" };
                Warehouses.AddRange(data.Warehouses.Select(w => w.WarehouseName));

                // Map Summary Cards
                SummaryCards = new List<UserSummaryCard>
                {
                    new UserSummaryCard { Title = "Total Users", Value = data.Summary.TotalUsers.ToString(), Icon = "groups", ColorClass = "primary" },
                    new UserSummaryCard { Title = "Active Users", Value = data.Summary.ActiveUsers.ToString(), Icon = "person_check", ColorClass = "success" },
                    new UserSummaryCard { Title = "Total Roles", Value = data.Summary.TotalRoles.ToString(), Icon = "security", ColorClass = "info" },
                    new UserSummaryCard { Title = "Inactive Users", Value = data.Summary.InactiveUsers.ToString(), Icon = "person_off", ColorClass = "warning" }
                };

                // Map User Rows
                Users = data.Users.Select(u => new UserRow
                {
                    UserID = u.UserID,
                    Username = u.Username,
                    RoleID = u.RoleID ?? 0,
                    Phone = u.Phone,
                    Code = $"USR-{u.UserID:D3}",
                    Name = u.FullName,
                    Email = u.Email,
                    Role = u.RoleName,
                    WarehouseAccess = "Assigned Locations", // Placeholder
                    TaskType = u.RoleName == "Admin" ? "System Owner" : (u.RoleName == "Manager" ? "Approvals" : "Operations"),
                    LastLogin = "Recent", // Placeholder
                    Status = u.IsActive ? "Active" : "Inactive",
                    StatusColor = u.IsActive ? "success" : "muted"
                }).ToList();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading data: " + ex.Message;
                Users = new List<UserRow>();
            }
        }
    }

    public class UserSummaryCard
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
    }

    public class UserRow
    {
        public int UserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public int RoleID { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string WarehouseAccess { get; set; } = string.Empty;
        public string TaskType { get; set; } = string.Empty;
        public string LastLogin { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
    }
}

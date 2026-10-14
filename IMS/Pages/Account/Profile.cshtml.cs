using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using IMS.Services;
using IMS.Repositories;
using IMS.Models.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace IMS.Pages.Account
{
    public class ProfileModel : PageModel
    {
        private readonly IUserContext _userContext;
        private readonly IUserRoleRepository _userRepository;
        private readonly IUserRoleService _userRoleService;
        private readonly IStockMovementService _stockMovementService;

        public ProfileModel(
            IUserContext userContext, 
            IUserRoleRepository userRepository,
            IUserRoleService userRoleService,
            IStockMovementService stockMovementService)
        {
            _userContext = userContext;
            _userRepository = userRepository;
            _userRoleService = userRoleService;
            _stockMovementService = stockMovementService;
        }

        public UserDto? UserProfile { get; set; }
        
        [TempData]
        public string? SuccessMessage { get; set; }
        
        [TempData]
        public string? ErrorMessage { get; set; }

        [BindProperty]
        public UpdateProfileInputModel UpdateProfileInput { get; set; } = new();

        [BindProperty]
        public ChangePasswordInputModel ChangePasswordInput { get; set; } = new();

        public List<StockMovementDto> RecentActivities { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (!_userContext.IsAuthenticated) return RedirectToPage("/Account/Login");
            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateProfileAsync()
        {
            if (!_userContext.IsAuthenticated) return RedirectToPage("/Account/Login");
            
            var userId = _userContext.CurrentUserID;
            var existingUser = await _userRepository.GetUserByIdAsync(userId);
            
            if (existingUser != null)
            {
                var dto = new UserUpdateDto
                {
                    UserID = userId,
                    FullName = UpdateProfileInput.FullName,
                    Email = UpdateProfileInput.Email,
                    Phone = UpdateProfileInput.Phone ?? string.Empty,
                    RoleID = existingUser.RoleID ?? 0,
                    IsActive = existingUser.IsActive,
                    SupplierID = existingUser.SupplierID
                };
                
                var result = await _userRoleService.UpdateUserAsync(dto);
                if (result.Success)
                    SuccessMessage = "Profile details updated successfully.";
                else
                    ErrorMessage = result.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            if (!_userContext.IsAuthenticated) return RedirectToPage("/Account/Login");

            if (ChangePasswordInput.NewPassword != ChangePasswordInput.ConfirmPassword)
            {
                ErrorMessage = "New Password and Confirm Password do not match.";
                return RedirectToPage();
            }

            var userId = _userContext.CurrentUserID;
            var result = await _userRoleService.ChangePasswordAsync(userId, ChangePasswordInput.CurrentPassword, ChangePasswordInput.NewPassword);
            
            if (result.Success)
                SuccessMessage = result.Message;
            else
                ErrorMessage = result.Message;

            return RedirectToPage();
        }

        private async Task LoadDataAsync()
        {
            var userId = _userContext.CurrentUserID;
            UserProfile = await _userRepository.GetUserByIdAsync(userId);
            
            if (UserProfile != null)
            {
                UpdateProfileInput.FullName = UserProfile.FullName;
                UpdateProfileInput.Email = UserProfile.Email;
                UpdateProfileInput.Phone = UserProfile.Phone;

                var movements = await _stockMovementService.GetRecentMovementsAsync(100);
                RecentActivities = movements.Where(m => m.PerformedBy == UserProfile.FullName).Take(15).ToList();
            }
        }

        public class UpdateProfileInputModel
        {
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
        }

        public class ChangePasswordInputModel
        {
            public string CurrentPassword { get; set; } = string.Empty;
            public string NewPassword { get; set; } = string.Empty;
            public string ConfirmPassword { get; set; } = string.Empty;
        }
    }
}

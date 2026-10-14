using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using IMS.Services;
using IMS.Models.DTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace IMS.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly IUserRoleService _userRoleService;
        private readonly ISystemSettingService _settingService;

        public LoginModel(IUserRoleService userRoleService, ISystemSettingService settingService)
        {
            _userRoleService = userRoleService;
            _settingService = settingService;
        }

        [BindProperty]
        public LoginInput Input { get; set; } = new();

        public string ErrorMessage { get; set; } = string.Empty;
        public string BrandName { get; set; } = "InventoryPro";
        public string BrandTagline { get; set; } = "Enterprise Logistics";

        public async Task OnGetAsync()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                Response.Redirect("/Index");
                return;
            }
            await LoadBrandingAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadBrandingAsync();
                return Page();
            }

            var user = await _userRoleService.AuthenticateAsync(Input.UsernameOrEmail, Input.Password);

            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.RoleName),
                    new Claim("Username", user.Username)
                };

                if (user.SupplierID.HasValue)
                {
                    claims.Add(new Claim("SupplierID", user.SupplierID.Value.ToString()));
                }

                var claimsIdentity = new ClaimsIdentity(claims, "IMS_Auth");

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = Input.RememberMe,
                    ExpiresUtc = System.DateTimeOffset.UtcNow.AddDays(7)
                };

                await HttpContext.SignInAsync("IMS_Auth", new ClaimsPrincipal(claimsIdentity), authProperties);

                return RedirectToPage("/Index");
            }

            ErrorMessage = "Invalid username or password.";
            await LoadBrandingAsync();
            return Page();
        }

        private async Task LoadBrandingAsync()
        {
            try
            {
                var settings = await _settingService.GetSettingsAsync();
                var name = settings.FirstOrDefault(s => s.SettingKey == "Company.Name")?.SettingValue;
                if (!string.IsNullOrWhiteSpace(name)) BrandName = name;

                var tagline = settings.FirstOrDefault(s => s.SettingKey == "Company.Tagline" || s.SettingKey == "Company.Subtitle")?.SettingValue;
                if (!string.IsNullOrWhiteSpace(tagline)) BrandTagline = tagline;
            }
            catch
            {
                // Fallback to defaults
            }
        }
    }

    public class LoginInput
    {
        public string UsernameOrEmail { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }
}

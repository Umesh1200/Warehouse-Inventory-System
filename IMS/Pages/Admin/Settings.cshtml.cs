using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using IMS.Services;
using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace IMS.Pages.Admin
{
    public class SettingsModel : PageModel
    {
        private readonly ISystemSettingService _settingService;
        private readonly IUserContext _userContext;

        public SettingsModel(ISystemSettingService settingService, IUserContext userContext)
        {
            _settingService = settingService;
            _userContext = userContext;
        }

        public List<SystemSettingDto> Settings { get; set; } = new();
        public string SuccessMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        [BindProperty]
        public List<SystemSettingUpdateDto> UpdatedSettings { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadSettingsAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadSettingsAsync();
                return Page();
            }

            var result = await _settingService.UpdateSettingsAsync(UpdatedSettings, _userContext.CurrentUserID);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage();
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadSettingsAsync();
                return Page();
            }
        }

        private async Task LoadSettingsAsync()
        {
            if (TempData["SuccessMessage"] != null)
                SuccessMessage = TempData["SuccessMessage"]?.ToString() ?? "";

            var allSettings = await _settingService.GetSettingsAsync();
            Settings = allSettings.ToList();
            
            // Initialize the bind property with current values
            UpdatedSettings = Settings.Select(s => new SystemSettingUpdateDto 
            { 
                SettingKey = s.SettingKey, 
                SettingValue = s.SettingValue 
            }).ToList();
        }
    }
}

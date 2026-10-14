using IMS.Models.DTOs;
using IMS.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.Services
{
    public class SystemSettingService : ISystemSettingService
    {
        private readonly ISystemSettingRepository _repository;

        public SystemSettingService(ISystemSettingRepository repository)
        {
            _repository = repository;
        }

        public async Task<IReadOnlyList<SystemSettingDto>> GetSettingsAsync()
        {
            var settings = await _repository.GetSettingsAsync();
            Console.WriteLine($"DEBUG: Retrieved {settings.Count} settings from repository.");
            foreach(var s in settings) Console.WriteLine($"DEBUG: Key: {s.SettingKey}");
            
            // If empty, ensure default keys exist in return list for UI binding
            var defaultKeys = new[] 
            { 
                "Company.Name", "Company.Address", "Company.Email", "Company.Phone",
                "Inventory.DefaultReorderLevel", "Inventory.LowStockThreshold", "Inventory.EnableNegativeStock",
                "Warehouse.DefaultQCRequired", "Production.ProductionPagesEnabled"
            };

            var result = settings.ToList();
            foreach (var key in defaultKeys)
            {
                if (!result.Any(s => s.SettingKey == key))
                {
                    result.Add(new SystemSettingDto 
                    { 
                        SettingKey = key, 
                        SettingValue = GetDefaultValue(key),
                        SettingGroup = key.Split('.')[0] 
                    });
                }
                else
                {
                    result.First(s => s.SettingKey == key).SettingGroup = key.Split('.')[0];
                }
            }

            return result.OrderBy(s => s.SettingGroup).ThenBy(s => s.SettingKey).ToList();
        }

        public async Task<SystemSettingActionResultDto> UpdateSettingsAsync(IReadOnlyList<SystemSettingUpdateDto> settings, int updatedBy)
        {
            foreach (var setting in settings)
            {
                if (string.IsNullOrWhiteSpace(setting.SettingKey))
                    return new SystemSettingActionResultDto { Success = false, Message = "Setting key is required." };

                // Validation
                if (setting.SettingKey == "Inventory.DefaultReorderLevel" || setting.SettingKey == "Inventory.LowStockThreshold")
                {
                    if (!decimal.TryParse(setting.SettingValue, out decimal val) || val < 0)
                        return new SystemSettingActionResultDto { Success = false, Message = $"{setting.SettingKey} must be a non-negative number." };
                }

                if (setting.SettingKey == "Inventory.EnableNegativeStock" && setting.SettingValue?.ToLower() == "true")
                {
                    return new SystemSettingActionResultDto { Success = false, Message = "Negative stock is not allowed per business rules." };
                }

                if (setting.SettingKey == "Company.Email" && !string.IsNullOrEmpty(setting.SettingValue) && !setting.SettingValue.Contains("@"))
                {
                    return new SystemSettingActionResultDto { Success = false, Message = "Invalid company email format." };
                }
            }

            return await _repository.UpdateSettingsAsync(settings, updatedBy);
        }

        private string GetDefaultValue(string key)
        {
            return key switch
            {
                "Company.Name" => "IMS Enterprise",
                "Inventory.DefaultReorderLevel" => "10",
                "Inventory.LowStockThreshold" => "5",
                "Inventory.EnableNegativeStock" => "false",
                "Warehouse.DefaultQCRequired" => "true",
                "Production.ProductionPagesEnabled" => "true",
                _ => ""
            };
        }
    }
}

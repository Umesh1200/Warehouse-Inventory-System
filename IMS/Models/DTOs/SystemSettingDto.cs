using System;
using System.Collections.Generic;

namespace IMS.Models.DTOs
{
    public class SystemSettingDto
    {
        public int SettingID { get; set; }
        public string SettingKey { get; set; } = string.Empty;
        public string? SettingValue { get; set; }
        public string SettingGroup { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string UpdatedByName { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }

    public class SystemSettingUpdateDto
    {
        public string SettingKey { get; set; } = string.Empty;
        public string? SettingValue { get; set; }
    }

    public class SystemSettingActionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

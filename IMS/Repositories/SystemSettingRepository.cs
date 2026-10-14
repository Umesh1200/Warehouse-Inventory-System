using IMS.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public class SystemSettingRepository : ISystemSettingRepository
    {
        private readonly string _connectionString;

        public SystemSettingRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<IReadOnlyList<SystemSettingDto>> GetSettingsAsync()
        {
            var results = new List<SystemSettingDto>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand("SELECT * FROM SystemSettings ORDER BY SettingKey", connection);
                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var key = reader.GetString("SettingKey");
                    var rawGroup = reader.IsDBNull("SettingGroup") ? "" : reader.GetString("SettingGroup");
                    var group = !string.IsNullOrWhiteSpace(rawGroup) ? rawGroup : (key.Contains('.') ? key.Split('.')[0] : "General");

                    results.Add(new SystemSettingDto
                    {
                        SettingID = reader.GetInt32("SettingID"),
                        SettingKey = key,
                        SettingValue = reader.IsDBNull("SettingValue") ? "" : reader.GetString("SettingValue"),
                        SettingGroup = group,
                        Description = reader.IsDBNull("Description") ? "" : reader.GetString("Description"),
                        UpdatedAt = reader.GetDateTime("UpdatedAt")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching system settings.", ex);
            }
            return results;
        }

        public async Task<SystemSettingActionResultDto> UpdateSettingsAsync(IReadOnlyList<SystemSettingUpdateDto> settings, int updatedBy)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                foreach (var setting in settings)
                {
                    using var command = new SqlCommand(@"
                        IF EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = @Key)
                            UPDATE SystemSettings SET SettingValue = @Value, UpdatedAt = GETDATE() WHERE SettingKey = @Key
                        ELSE
                            INSERT INTO SystemSettings (SettingKey, SettingValue, SettingGroup, UpdatedAt) 
                            VALUES (@Key, @Value, CASE WHEN CHARINDEX('.', @Key) > 0 THEN SUBSTRING(@Key, 1, CHARINDEX('.', @Key) - 1) ELSE 'General' END, GETDATE())", 
                        connection, transaction);
                    
                    command.Parameters.AddWithValue("@Key", setting.SettingKey);
                    command.Parameters.AddWithValue("@Value", setting.SettingValue ?? (object)DBNull.Value);
                    await command.ExecuteNonQueryAsync();
                }
                await transaction.CommitAsync();
                return new SystemSettingActionResultDto { Success = true, Message = "Settings updated successfully." };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new SystemSettingActionResultDto { Success = false, Message = "Error updating settings: " + ex.Message };
            }
        }
    }
}

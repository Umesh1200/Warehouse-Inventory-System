using IMS.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public class SupplierRepository : ISupplierRepository
    {
        private readonly string _connectionString;

        public SupplierRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<IEnumerable<SupplierDto>> GetAllSuppliersAsync()
        {
            var suppliers = new List<SupplierDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT * FROM Suppliers 
                    ORDER BY SupplierName", connection);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    suppliers.Add(new SupplierDto
                    {
                        SupplierID = reader.GetInt32("SupplierID"),
                        SupplierCode = reader.GetString("SupplierCode"),
                        SupplierName = reader.GetString("SupplierName"),
                        ContactPerson = reader.IsDBNull("ContactPerson") ? "" : reader.GetString("ContactPerson"),
                        Phone = reader.IsDBNull("Phone") ? "" : reader.GetString("Phone"),
                        Email = reader.IsDBNull("Email") ? "" : reader.GetString("Email"),
                        Address = reader.IsDBNull("Address") ? "" : reader.GetString("Address"),
                        SupplierType = reader.IsDBNull("SupplierType") ? "" : reader.GetString("SupplierType"),
                        LeadTimeDays = reader.IsDBNull("LeadTimeDays") ? 0 : reader.GetInt32("LeadTimeDays"),
                        PaymentTerms = reader.IsDBNull("PaymentTerms") ? "" : reader.GetString("PaymentTerms"),
                        Status = reader.IsDBNull("Status") ? "Active" : reader.GetString("Status"),
                        ReturnPolicy = reader.IsDBNull("ReturnPolicy") ? "" : reader.GetString("ReturnPolicy"),
                        ReturnEmail = reader.IsDBNull("ReturnEmail") ? "" : reader.GetString("ReturnEmail"),
                        PreferredCarrier = reader.IsDBNull("PreferredCarrier") ? "" : reader.GetString("PreferredCarrier"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        UpdatedAt = reader.GetDateTime("UpdatedAt")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching suppliers.", ex);
            }

            return suppliers;
        }

        public async Task<int> CreateSupplierAsync(SupplierCreateDto supplier)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    INSERT INTO Suppliers (
                        SupplierCode, SupplierName, ContactPerson, Phone, Email, 
                        Address, SupplierType, LeadTimeDays, PaymentTerms, Status,
                        ReturnPolicy, ReturnEmail, PreferredCarrier
                    )
                    VALUES (
                        @SupplierCode, @SupplierName, @ContactPerson, @Phone, @Email, 
                        @Address, @SupplierType, @LeadTimeDays, @PaymentTerms, @Status,
                        @ReturnPolicy, @ReturnEmail, @PreferredCarrier
                    );
                    SELECT SCOPE_IDENTITY();", connection);

                command.Parameters.AddWithValue("@SupplierCode", supplier.SupplierCode);
                command.Parameters.AddWithValue("@SupplierName", supplier.SupplierName);
                command.Parameters.AddWithValue("@ContactPerson", supplier.ContactPerson ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Phone", supplier.Phone ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Email", supplier.Email ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Address", supplier.Address ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SupplierType", supplier.SupplierType ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@LeadTimeDays", supplier.LeadTimeDays);
                command.Parameters.AddWithValue("@PaymentTerms", supplier.PaymentTerms ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Status", supplier.Status);
                command.Parameters.AddWithValue("@ReturnPolicy", supplier.ReturnPolicy ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ReturnEmail", supplier.ReturnEmail ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PreferredCarrier", supplier.PreferredCarrier ?? (object)DBNull.Value);

                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                throw new Exception("Supplier Code already exists.");
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while creating supplier.", ex);
            }
        }

        public async Task<SupplierDto?> GetSupplierByIdAsync(int supplierId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand("SELECT * FROM Suppliers WHERE SupplierID = @SupplierID", connection);
                command.Parameters.AddWithValue("@SupplierID", supplierId);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new SupplierDto
                    {
                        SupplierID = reader.GetInt32("SupplierID"),
                        SupplierCode = reader.GetString("SupplierCode"),
                        SupplierName = reader.GetString("SupplierName"),
                        ContactPerson = reader.IsDBNull("ContactPerson") ? "" : reader.GetString("ContactPerson"),
                        Phone = reader.IsDBNull("Phone") ? "" : reader.GetString("Phone"),
                        Email = reader.IsDBNull("Email") ? "" : reader.GetString("Email"),
                        Address = reader.IsDBNull("Address") ? "" : reader.GetString("Address"),
                        SupplierType = reader.IsDBNull("SupplierType") ? "" : reader.GetString("SupplierType"),
                        LeadTimeDays = reader.IsDBNull("LeadTimeDays") ? 0 : reader.GetInt32("LeadTimeDays"),
                        PaymentTerms = reader.IsDBNull("PaymentTerms") ? "" : reader.GetString("PaymentTerms"),
                        Status = reader.IsDBNull("Status") ? "Active" : reader.GetString("Status"),
                        ReturnPolicy = reader.IsDBNull("ReturnPolicy") ? "" : reader.GetString("ReturnPolicy"),
                        ReturnEmail = reader.IsDBNull("ReturnEmail") ? "" : reader.GetString("ReturnEmail"),
                        PreferredCarrier = reader.IsDBNull("PreferredCarrier") ? "" : reader.GetString("PreferredCarrier"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        UpdatedAt = reader.GetDateTime("UpdatedAt")
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching the supplier.", ex);
            }
        }

        public async Task<bool> UpdateSupplierAsync(SupplierUpdateDto supplier)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    UPDATE Suppliers SET 
                        SupplierCode = @SupplierCode, 
                        SupplierName = @SupplierName, 
                        ContactPerson = @ContactPerson, 
                        Phone = @Phone, 
                        Email = @Email, 
                        Address = @Address, 
                        SupplierType = @SupplierType, 
                        LeadTimeDays = @LeadTimeDays, 
                        PaymentTerms = @PaymentTerms, 
                        Status = @Status,
                        ReturnPolicy = @ReturnPolicy,
                        ReturnEmail = @ReturnEmail,
                        PreferredCarrier = @PreferredCarrier,
                        UpdatedAt = GETDATE()
                    WHERE SupplierID = @SupplierID", connection);

                command.Parameters.AddWithValue("@SupplierID", supplier.SupplierID);
                command.Parameters.AddWithValue("@SupplierCode", supplier.SupplierCode);
                command.Parameters.AddWithValue("@SupplierName", supplier.SupplierName);
                command.Parameters.AddWithValue("@ContactPerson", supplier.ContactPerson ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Phone", supplier.Phone ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Email", supplier.Email ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Address", supplier.Address ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SupplierType", supplier.SupplierType ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@LeadTimeDays", supplier.LeadTimeDays);
                command.Parameters.AddWithValue("@PaymentTerms", supplier.PaymentTerms ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Status", supplier.Status);
                command.Parameters.AddWithValue("@ReturnPolicy", supplier.ReturnPolicy ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ReturnEmail", supplier.ReturnEmail ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PreferredCarrier", supplier.PreferredCarrier ?? (object)DBNull.Value);

                await connection.OpenAsync();
                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                throw new Exception("Supplier Code already exists.");
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while updating the supplier.", ex);
            }
        }

        public async Task<bool> DeactivateSupplierAsync(int supplierId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    UPDATE Suppliers 
                    SET Status = 'Inactive', UpdatedAt = GETDATE()
                    WHERE SupplierID = @SupplierID", connection);

                command.Parameters.AddWithValue("@SupplierID", supplierId);

                await connection.OpenAsync();
                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while deactivating the supplier.", ex);
            }
        }

        public async Task<bool> IsSupplierCodeExistsAsync(string supplierCode)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT COUNT(*) FROM Suppliers WHERE SupplierCode = @SupplierCode", connection);
            command.Parameters.AddWithValue("@SupplierCode", supplierCode);
            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            var count = result != null ? (int)result : 0;
            return count > 0;
        }

        public async Task<bool> IsSupplierCodeExistsForOtherSupplierAsync(string supplierCode, int supplierId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT COUNT(*) FROM Suppliers WHERE SupplierCode = @SupplierCode AND SupplierID != @SupplierID", connection);
            command.Parameters.AddWithValue("@SupplierCode", supplierCode);
            command.Parameters.AddWithValue("@SupplierID", supplierId);
            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            var count = result != null ? (int)result : 0;
            return count > 0;
        }
    }
}

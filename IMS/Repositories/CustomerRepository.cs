using IMS.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly string _connectionString;

        public CustomerRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<IEnumerable<CustomerDto>> GetAllCustomersWithStatsAsync()
        {
            var customers = new List<CustomerDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT c.*, 
                           (SELECT COUNT(*) FROM SalesOrders so WHERE so.CustomerID = c.CustomerID AND so.Status NOT IN ('Fulfilled', 'Cancelled')) AS PendingOrderCount,
                           (SELECT SUM(so.TotalAmount) FROM SalesOrders so WHERE so.CustomerID = c.CustomerID AND so.Status = 'Fulfilled') AS TotalSalesAmount
                    FROM Customers c
                    ORDER BY c.CustomerName", connection);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    customers.Add(new CustomerDto
                    {
                        CustomerID = reader.GetInt32("CustomerID"),
                        CustomerCode = reader.GetString("CustomerCode"),
                        CustomerName = reader.GetString("CustomerName"),
                        ContactPerson = reader.IsDBNull("ContactPerson") ? "" : reader.GetString("ContactPerson"),
                        Phone = reader.IsDBNull("Phone") ? "" : reader.GetString("Phone"),
                        Email = reader.IsDBNull("Email") ? "" : reader.GetString("Email"),
                        Address = reader.IsDBNull("Address") ? "" : reader.GetString("Address"),
                        CustomerType = reader.IsDBNull("CustomerType") ? "" : reader.GetString("CustomerType"),
                        Status = reader.IsDBNull("Status") ? "Active" : reader.GetString("Status"),
                        PendingOrderCount = reader.GetInt32("PendingOrderCount"),
                        TotalSalesAmount = reader.IsDBNull("TotalSalesAmount") ? 0 : reader.GetDecimal("TotalSalesAmount"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        UpdatedAt = reader.GetDateTime("UpdatedAt")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching customers.", ex);
            }

            return customers;
        }

        public async Task<int> CreateCustomerAsync(CustomerCreateDto customer)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    INSERT INTO Customers (
                        CustomerCode, CustomerName, ContactPerson, Phone, Email, 
                        Address, CustomerType, Status
                    )
                    VALUES (
                        @CustomerCode, @CustomerName, @ContactPerson, @Phone, @Email, 
                        @Address, @CustomerType, @Status
                    );
                    SELECT SCOPE_IDENTITY();", connection);

                command.Parameters.AddWithValue("@CustomerCode", customer.CustomerCode);
                command.Parameters.AddWithValue("@CustomerName", customer.CustomerName);
                command.Parameters.AddWithValue("@ContactPerson", customer.ContactPerson ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Phone", customer.Phone ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Email", customer.Email ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Address", customer.Address ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@CustomerType", customer.CustomerType ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Status", customer.Status);

                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                throw new Exception("Customer Code already exists.");
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while creating customer.", ex);
            }
        }

        public async Task<CustomerDto?> GetCustomerByIdAsync(int customerId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand("SELECT * FROM Customers WHERE CustomerID = @CustomerID", connection);
                command.Parameters.AddWithValue("@CustomerID", customerId);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new CustomerDto
                    {
                        CustomerID = reader.GetInt32("CustomerID"),
                        CustomerCode = reader.GetString("CustomerCode"),
                        CustomerName = reader.GetString("CustomerName"),
                        ContactPerson = reader.IsDBNull("ContactPerson") ? "" : reader.GetString("ContactPerson"),
                        Phone = reader.IsDBNull("Phone") ? "" : reader.GetString("Phone"),
                        Email = reader.IsDBNull("Email") ? "" : reader.GetString("Email"),
                        Address = reader.IsDBNull("Address") ? "" : reader.GetString("Address"),
                        CustomerType = reader.IsDBNull("CustomerType") ? "" : reader.GetString("CustomerType"),
                        Status = reader.IsDBNull("Status") ? "Active" : reader.GetString("Status"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        UpdatedAt = reader.GetDateTime("UpdatedAt")
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching the customer.", ex);
            }
        }

        public async Task<bool> UpdateCustomerAsync(CustomerUpdateDto customer)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    UPDATE Customers SET 
                        CustomerCode = @CustomerCode, 
                        CustomerName = @CustomerName, 
                        ContactPerson = @ContactPerson, 
                        Phone = @Phone, 
                        Email = @Email, 
                        Address = @Address, 
                        CustomerType = @CustomerType, 
                        Status = @Status,
                        UpdatedAt = GETDATE()
                    WHERE CustomerID = @CustomerID", connection);

                command.Parameters.AddWithValue("@CustomerID", customer.CustomerID);
                command.Parameters.AddWithValue("@CustomerCode", customer.CustomerCode);
                command.Parameters.AddWithValue("@CustomerName", customer.CustomerName);
                command.Parameters.AddWithValue("@ContactPerson", customer.ContactPerson ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Phone", customer.Phone ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Email", customer.Email ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Address", customer.Address ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@CustomerType", customer.CustomerType ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Status", customer.Status);

                await connection.OpenAsync();
                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                throw new Exception("Customer Code already exists.");
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while updating the customer.", ex);
            }
        }

        public async Task<bool> DeactivateCustomerAsync(int customerId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    UPDATE Customers 
                    SET Status = 'Inactive', UpdatedAt = GETDATE()
                    WHERE CustomerID = @CustomerID", connection);

                command.Parameters.AddWithValue("@CustomerID", customerId);

                await connection.OpenAsync();
                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while deactivating the customer.", ex);
            }
        }

        public async Task<bool> IsCustomerCodeExistsAsync(string customerCode)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT COUNT(*) FROM Customers WHERE CustomerCode = @CustomerCode", connection);
            command.Parameters.AddWithValue("@CustomerCode", customerCode);
            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            var count = result != null ? (int)result : 0;
            return count > 0;
        }

        public async Task<bool> IsCustomerCodeExistsForOtherCustomerAsync(string customerCode, int customerId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT COUNT(*) FROM Customers WHERE CustomerCode = @CustomerCode AND CustomerID != @CustomerID", connection);
            command.Parameters.AddWithValue("@CustomerCode", customerCode);
            command.Parameters.AddWithValue("@CustomerID", customerId);
            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            var count = result != null ? (int)result : 0;
            return count > 0;
        }
    }
}

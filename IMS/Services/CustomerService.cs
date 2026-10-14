using IMS.Models.DTOs;
using IMS.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;

        public CustomerService(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public async Task<IEnumerable<CustomerDto>> GetCustomersAsync()
        {
            return await _customerRepository.GetAllCustomersWithStatsAsync();
        }

        public async Task<CustomerSummaryDto> GetCustomerSummaryAsync()
        {
            var customers = await _customerRepository.GetAllCustomersWithStatsAsync();
            var customersList = customers.ToList();

            return new CustomerSummaryDto
            {
                TotalCustomers = customersList.Count,
                ActiveCustomers = customersList.Count(c => c.Status == "Active"),
                WholesaleCustomers = customersList.Count(c => c.CustomerType == "Wholesale"),
                TotalPendingOrders = customersList.Sum(c => c.PendingOrderCount)
            };
        }

        public async Task<CustomerActionResultDto> CreateCustomerAsync(CustomerCreateDto customer)
        {
            if (string.IsNullOrWhiteSpace(customer.CustomerCode)) return new CustomerActionResultDto { Success = false, Message = "Customer Code is required." };
            if (string.IsNullOrWhiteSpace(customer.CustomerName)) return new CustomerActionResultDto { Success = false, Message = "Customer Name is required." };
            
            if (!string.IsNullOrWhiteSpace(customer.Email) && !customer.Email.Contains("@")) 
                return new CustomerActionResultDto { Success = false, Message = "Invalid email format." };

            if (await _customerRepository.IsCustomerCodeExistsAsync(customer.CustomerCode))
            {
                return new CustomerActionResultDto { Success = false, Message = $"Customer Code '{customer.CustomerCode}' already exists." };
            }

            try
            {
                int newId = await _customerRepository.CreateCustomerAsync(customer);
                return new CustomerActionResultDto { Success = true, Message = "Customer created successfully.", NewID = newId };
            }
            catch (Exception ex)
            {
                return new CustomerActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<CustomerActionResultDto> UpdateCustomerAsync(CustomerUpdateDto customer)
        {
            if (customer.CustomerID <= 0) return new CustomerActionResultDto { Success = false, Message = "Invalid Customer ID." };
            if (string.IsNullOrWhiteSpace(customer.CustomerCode)) return new CustomerActionResultDto { Success = false, Message = "Customer Code is required." };
            if (string.IsNullOrWhiteSpace(customer.CustomerName)) return new CustomerActionResultDto { Success = false, Message = "Customer Name is required." };

            if (!string.IsNullOrWhiteSpace(customer.Email) && !customer.Email.Contains("@")) 
                return new CustomerActionResultDto { Success = false, Message = "Invalid email format." };

            if (await _customerRepository.IsCustomerCodeExistsForOtherCustomerAsync(customer.CustomerCode, customer.CustomerID))
            {
                return new CustomerActionResultDto { Success = false, Message = $"Customer Code '{customer.CustomerCode}' already exists." };
            }

            try
            {
                bool success = await _customerRepository.UpdateCustomerAsync(customer);
                if (success)
                    return new CustomerActionResultDto { Success = true, Message = "Customer updated successfully." };
                else
                    return new CustomerActionResultDto { Success = false, Message = "Customer not found or no changes made." };
            }
            catch (Exception ex)
            {
                return new CustomerActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<CustomerActionResultDto> DeactivateCustomerAsync(int customerId)
        {
            if (customerId <= 0) return new CustomerActionResultDto { Success = false, Message = "Invalid Customer ID." };

            try
            {
                bool success = await _customerRepository.DeactivateCustomerAsync(customerId);
                if (success)
                    return new CustomerActionResultDto { Success = true, Message = "Customer deactivated successfully." };
                else
                    return new CustomerActionResultDto { Success = false, Message = "Customer not found or already inactive." };
            }
            catch (Exception ex)
            {
                return new CustomerActionResultDto { Success = false, Message = ex.Message };
            }
        }
    }
}

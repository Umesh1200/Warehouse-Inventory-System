using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public interface ICustomerRepository
    {
        Task<IEnumerable<CustomerDto>> GetAllCustomersWithStatsAsync();
        Task<int> CreateCustomerAsync(CustomerCreateDto customer);
        Task<CustomerDto?> GetCustomerByIdAsync(int customerId);
        Task<bool> UpdateCustomerAsync(CustomerUpdateDto customer);
        Task<bool> DeactivateCustomerAsync(int customerId);
        Task<bool> IsCustomerCodeExistsAsync(string customerCode);
        Task<bool> IsCustomerCodeExistsForOtherCustomerAsync(string customerCode, int customerId);
    }
}

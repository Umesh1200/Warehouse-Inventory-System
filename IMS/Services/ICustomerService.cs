using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Services
{
    public interface ICustomerService
    {
        Task<IEnumerable<CustomerDto>> GetCustomersAsync();
        Task<CustomerSummaryDto> GetCustomerSummaryAsync();
        Task<CustomerActionResultDto> CreateCustomerAsync(CustomerCreateDto customer);
        Task<CustomerActionResultDto> UpdateCustomerAsync(CustomerUpdateDto customer);
        Task<CustomerActionResultDto> DeactivateCustomerAsync(int customerId);
    }
}

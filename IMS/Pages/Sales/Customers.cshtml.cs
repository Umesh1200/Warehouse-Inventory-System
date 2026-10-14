using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using IMS.Services;
using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace IMS.Pages.Sales
{
    public class CustomersModel : PageModel
    {
        private readonly ICustomerService _customerService;
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public CustomersModel(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        public List<CustomerSummaryCard> SummaryCards { get; set; } = new();
        public List<CustomerRow> Customers { get; set; } = new();
        public List<string> CustomerTypes { get; set; } = new();

        [BindProperty]
        public CustomerCreateDto NewCustomer { get; set; } = new();

        [BindProperty]
        public CustomerUpdateDto EditCustomer { get; set; } = new();

        [BindProperty]
        public int DeactivateCustomerID { get; set; }

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return Page();
            }

            var result = await _customerService.CreateCustomerAsync(NewCustomer);
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage();
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadDataAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return Page();
            }

            var result = await _customerService.UpdateCustomerAsync(EditCustomer);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage();
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadDataAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeactivateAsync()
        {
            if (DeactivateCustomerID <= 0)
            {
                ErrorMessage = "Invalid Customer ID for deactivation.";
                await LoadDataAsync();
                return Page();
            }

            var result = await _customerService.DeactivateCustomerAsync(DeactivateCustomerID);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage();
            }
            else
            {
                ErrorMessage = result.Message;
                await LoadDataAsync();
                return Page();
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                if (TempData["SuccessMessage"] != null)
                {
                    SuccessMessage = TempData["SuccessMessage"]?.ToString() ?? "";
                }

                CustomerTypes = new List<string> { "All Types", "Retail", "Wholesale", "Distributor", "Outlet" };

                var customersData = (await _customerService.GetCustomersAsync()).ToList();
                var summaryData = await _customerService.GetCustomerSummaryAsync();
                var today = DateTime.Today;
                var lastSevenDays = Enumerable.Range(6, 7)
                    .Select(offset => today.AddDays(-offset))
                    .ToList();

                SummaryCards = new List<CustomerSummaryCard>
                {
                    BuildSummaryCard("Total Customers", summaryData.TotalCustomers, "groups", "primary", customersData, lastSevenDays, _ => true),
                    BuildSummaryCard("Active Customers", summaryData.ActiveCustomers, "person_check", "success", customersData, lastSevenDays, c => c.Status == "Active"),
                    BuildSummaryCard("Wholesale", summaryData.WholesaleCustomers, "storefront", "info", customersData, lastSevenDays, c => c.CustomerType == "Wholesale"),
                    BuildSummaryCard("Pending Orders", summaryData.TotalPendingOrders, "shopping_bag", "warning", customersData, lastSevenDays, c => c.PendingOrderCount > 0)
                };

                // Map Customer Rows
                Customers = customersData.Select(c => new CustomerRow
                {
                    CustomerID = c.CustomerID,
                    Code = c.CustomerCode,
                    Name = c.CustomerName,
                    ContactPerson = c.ContactPerson,
                    Phone = c.Phone,
                    Email = c.Email,
                    Location = c.Address,
                    Type = c.CustomerType,
                    PendingOrders = c.PendingOrderCount,
                    TotalSales = $"NPR {c.TotalSalesAmount:N0}",
                    Status = c.Status
                }).ToList();
            }
            catch (Exception)
            {
                ErrorMessage = "Database connection failed. Showing offline mode.";
                Customers = new List<CustomerRow>();
            }
        }

        private CustomerSummaryCard BuildSummaryCard(string title, int value, string icon, string colorClass, List<CustomerDto> customers, List<DateTime> days, Func<CustomerDto, bool> predicate)
        {
            var dailyValues = days
                .Select(day => customers.Count(customer => customer.CreatedAt.Date == day.Date && predicate(customer)))
                .ToList();

            var max = Math.Max(1, dailyValues.Max());
            var points = dailyValues.Select((count, index) =>
            {
                var x = index * 16;
                var y = 36 - ((decimal)count / max * 30);
                return $"{x:0.##},{y:0.##}";
            });

            return new CustomerSummaryCard
            {
                Title = title,
                Value = value.ToString("N0"),
                Icon = icon,
                ColorClass = colorClass,
                TrendPoints = string.Join(" ", points),
                TrendLabel = $"{dailyValues.Sum():N0} new in last 7 days"
            };
        }
    }

    public class CustomerSummaryCard
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
        public string TrendPoints { get; set; } = string.Empty;
        public string TrendLabel { get; set; } = string.Empty;
    }

    public class CustomerRow
    {
        public int CustomerID { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int PendingOrders { get; set; }
        public string TotalSales { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}

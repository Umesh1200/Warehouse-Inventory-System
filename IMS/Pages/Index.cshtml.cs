using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using IMS.Services;

namespace IMS.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IUserContext _userContext;

        public IndexModel(IUserContext userContext)
        {
            _userContext = userContext;
        }

        public IActionResult OnGet()
        {
            if (!_userContext.IsAuthenticated)
            {
                return RedirectToPage("/Account/Login");
            }

            return _userContext.Role switch
            {
                "Admin" => RedirectToPage("/Dashboard/Admin"),
                "Manager" => RedirectToPage("/Dashboard/Admin"),
                "Warehouse Staff" => RedirectToPage("/WarehouseOperations/Receiving"),
                _ => RedirectToPage("/Dashboard/Admin")
            };
        }
    }
}

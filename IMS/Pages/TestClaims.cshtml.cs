using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace IMS.Pages
{
    [Authorize]
    public class TestClaimsModel : PageModel
    {
        public void OnGet() { }
    }
}

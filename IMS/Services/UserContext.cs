using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace IMS.Services
{
    public interface IUserContext
    {
        int CurrentUserID { get; }
        string FullName { get; }
        string Username { get; }
        string Role { get; }
        bool IsAuthenticated { get; }
        int? SupplierID { get; }
    }

    public class UserContext : IUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int CurrentUserID
        {
            get
            {
                var idClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
                return idClaim != null ? int.Parse(idClaim.Value) : 0;
            }
        }

        public string FullName => _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "Guest";

        public string Username => _httpContextAccessor.HttpContext?.User?.FindFirst("Username")?.Value ?? "";

        public string Role => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value ?? "";

        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        public int? SupplierID
        {
            get
            {
                var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("SupplierID");
                return claim != null ? int.Parse(claim.Value) : (int?)null;
            }
        }
    }
}

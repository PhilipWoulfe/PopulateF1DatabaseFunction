using F1.Core.Interfaces;
using F1.Core.Models;
using System.Security.Claims;

namespace F1.Api.Services
{
    public class UserContext : IUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public User? GetCurrentUser()
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            var email = principal?.FindFirstValue(ClaimTypes.Email)
                ?? _httpContextAccessor.HttpContext?.Request.Headers["Cf-Access-Authenticated-User-Email"].FirstOrDefault();

            if (email == null)
            {
                return null;
            }

            var user = new User
            {
                Email = email,
                IsAdmin = principal?.IsInRole("Admin") ?? false
            };

            return user;
        }
    }
}

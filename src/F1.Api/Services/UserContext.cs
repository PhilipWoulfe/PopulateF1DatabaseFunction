using F1.Core.Interfaces;
using F1.Core.Models;

namespace F1.Api.Services
{
    public class UserContext : IUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public UserContext(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        public User? GetCurrentUser()
        {
            var email = _httpContextAccessor.HttpContext?.Request.Headers["Cf-Access-Authenticated-User-Email"].FirstOrDefault();

            if (email == null)
            {
                return null;
            }

            var adminEmail = _configuration["AdminEmail"];
            var user = new User
            {
                Email = email,
                IsAdmin = email == adminEmail
            };

            return user;
        }
    }
}

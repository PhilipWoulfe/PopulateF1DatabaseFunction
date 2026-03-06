using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace F1.Web.Services
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IUserSession _userSession;
        private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        public CustomAuthenticationStateProvider(IUserSession userSession)
        {
            _userSession = userSession;
            _userSession.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(UserSession.User))
                {
                    NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
                }
            };
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (_userSession.User == null || _userSession.User.Email == null)
            {
                return Task.FromResult(new AuthenticationState(_anonymous));
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, _userSession.User.Email)
            };

            var displayName = string.IsNullOrWhiteSpace(_userSession.User.Name)
                ? _userSession.User.Email
                : _userSession.User.Name;

            if (!string.IsNullOrWhiteSpace(displayName))
            {
                claims.Add(new Claim(ClaimTypes.Name, displayName));
            }

            if (_userSession.User.IsAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            var identity = new ClaimsIdentity(claims, "Cloudflare");
            var user = new ClaimsPrincipal(identity);

            return Task.FromResult(new AuthenticationState(user));
        }
    }
}

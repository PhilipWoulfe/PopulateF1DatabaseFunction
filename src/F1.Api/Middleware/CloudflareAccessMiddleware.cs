using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace F1.Api.Middleware
{
    public class CloudflareAccessMiddleware
    {
        private readonly RequestDelegate _next;
        private const string AdminEmail = "philip.woulfe@gmail.com";

        public CloudflareAccessMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue("Cf-Access-Authenticated-User-Email", out var emailValues))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized: Cf-Access-Authenticated-User-Email header is missing.");
                return;
            }

            var email = emailValues.ToString();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email)
            };

            if (string.Equals(email, AdminEmail, System.StringComparison.OrdinalIgnoreCase))
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            var identity = new ClaimsIdentity(claims, "Cloudflare");
            context.User = new ClaimsPrincipal(identity);

            await _next(context);
        }
    }
}

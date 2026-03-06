using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace F1.Api.Middleware
{
    public class CloudflareAccessMiddleware
    {
        private readonly RequestDelegate _next;
        private const string AdminEmail = "philip.woulfe@gmail.com";
        private readonly IConfiguration _configuration;

        public CloudflareAccessMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var devSettings = _configuration.GetSection("DevSettings");
            if (devSettings.Exists() && devSettings.GetValue<bool>("SimulateCloudflare"))
            {
                var mockEmail = devSettings.GetValue<string>("MockEmail");
                if (!string.IsNullOrEmpty(mockEmail))
                {
                    context.Request.Headers["Cf-Access-Authenticated-User-Email"] = mockEmail;
                }
            }

            var email = context.Request.Headers["Cf-Access-Authenticated-User-Email"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(email))
            {
                var jwtAssertion = context.Request.Headers["Cf-Access-Jwt-Assertion"].FirstOrDefault();
                email = GetJwtClaim(jwtAssertion, "email");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized: Cloudflare Access identity headers are missing.");
                return;
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email)
            };

            var userId = context.Request.Headers["Cf-Access-Authenticated-User-Id"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            }

            var name = GetJwtClaim(context.Request.Headers["Cf-Access-Jwt-Assertion"].FirstOrDefault(), "name");
            var displayName = !string.IsNullOrWhiteSpace(name) ? name : email.Split('@')[0];
            claims.Add(new Claim(ClaimTypes.Name, displayName));

            if (string.Equals(email, AdminEmail, System.StringComparison.OrdinalIgnoreCase))
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            var identity = new ClaimsIdentity(claims, "Cloudflare");
            context.User = new ClaimsPrincipal(identity);

            await _next(context);
        }

        private static string? GetJwtClaim(string? jwt, string claimName)
        {
            if (string.IsNullOrWhiteSpace(jwt))
            {
                return null;
            }

            var parts = jwt.Split('.');
            if (parts.Length < 2)
            {
                return null;
            }

            try
            {
                var payload = parts[1]
                    .Replace('-', '+')
                    .Replace('_', '/');

                var mod4 = payload.Length % 4;
                if (mod4 > 0)
                {
                    payload = payload.PadRight(payload.Length + (4 - mod4), '=');
                }

                var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty(claimName, out var claimValue) && claimValue.ValueKind == JsonValueKind.String)
                {
                    return claimValue.GetString();
                }
            }
            catch
            {
                return null;
            }

            return null;
        }
    }
}

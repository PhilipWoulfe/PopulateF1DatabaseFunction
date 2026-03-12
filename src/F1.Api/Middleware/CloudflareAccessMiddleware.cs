using F1.Api.Services;
using System.Security.Claims;

namespace F1.Api.Middleware
{
    public class CloudflareAccessMiddleware
    {
        private const string UnauthorizedResponseMessage = "Unauthorized.";
        private readonly RequestDelegate _next;
        private const string DefaultAdminEmail = "philip.woulfe@gmail.com";
        private readonly IConfiguration _configuration;
        private readonly ICloudflareJwtValidator _jwtValidator;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly string _adminEmail;

        public CloudflareAccessMiddleware(
            RequestDelegate next,
            IConfiguration configuration,
            ICloudflareJwtValidator jwtValidator,
            IHostEnvironment hostEnvironment)
        {
            _next = next;
            _configuration = configuration;
            _jwtValidator = jwtValidator;
            _hostEnvironment = hostEnvironment;
            _adminEmail = _configuration["AdminEmail"] ?? DefaultAdminEmail;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var devSettings = _configuration.GetSection("DevSettings");
            if (_hostEnvironment.IsDevelopment() && devSettings.Exists() && devSettings.GetValue<bool>("SimulateCloudflare"))
            {
                var mockEmail = devSettings.GetValue<string>("MockEmail");
                if (!string.IsNullOrEmpty(mockEmail))
                {
                    context.User = BuildPrincipal(mockEmail, mockEmail.Split('@')[0], "dev-mock-user", _adminEmail);
                    await _next(context);
                    return;
                }
            }

            var jwtAssertion = context.Request.Headers["Cf-Access-Jwt-Assertion"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(jwtAssertion))
            {
                // Never allow legacy plaintext header identity outside development.
                if (_hostEnvironment.IsDevelopment() && _configuration.GetValue<bool>("CloudflareAccess:AllowLegacyHeaderBypass"))
                {
                    var fallbackEmail = context.Request.Headers["Cf-Access-Authenticated-User-Email"].FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(fallbackEmail))
                    {
                        context.User = BuildPrincipal(
                            fallbackEmail,
                            fallbackEmail.Split('@')[0],
                            context.Request.Headers["Cf-Access-Authenticated-User-Id"].FirstOrDefault(),
                            _adminEmail
                        );
                        await _next(context);
                        return;
                    }
                }

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync(UnauthorizedResponseMessage);
                return;
            }

            var validation = await _jwtValidator.ValidateAsync(jwtAssertion, context.RequestAborted);
            if (!validation.IsValid || validation.Principal is null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync(UnauthorizedResponseMessage);
                return;
            }

            var email = validation.Principal.FindFirst("email")?.Value
                ?? validation.Principal.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrWhiteSpace(email))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync(UnauthorizedResponseMessage);
                return;
            }

            var name = validation.Principal.FindFirst("name")?.Value
                ?? validation.Principal.FindFirst(ClaimTypes.Name)?.Value;
            var subject = validation.Principal.FindFirst("sub")?.Value
                ?? validation.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            context.User = BuildPrincipal(email, name, subject, _adminEmail);

            await _next(context);
        }

        private static ClaimsPrincipal BuildPrincipal(string email, string? name, string? userId, string adminEmail)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Email, email),
                new(ClaimTypes.Name, !string.IsNullOrWhiteSpace(name) ? name : email.Split('@')[0])
            };

            if (!string.IsNullOrWhiteSpace(userId))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            }

            if (string.Equals(email, adminEmail, System.StringComparison.OrdinalIgnoreCase))
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            return new ClaimsPrincipal(new ClaimsIdentity(claims, "Cloudflare"));
        }
    }
}

using F1.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;
using System.Text.Json;

namespace F1.Api.Middleware
{
    public class CloudflareAccessMiddleware
    {
        private const string UnauthorizedResponseMessage = "Unauthorized.";
        private readonly RequestDelegate _next;

        /// <summary>
        /// Returns a redacted representation of an email address to avoid logging full PII.
        /// Examples: "user@example.com" -> "u***@example.com".
        /// If the input is null/empty or not in the expected format, returns a non-sensitive placeholder.
        /// </summary>
        private static string RedactEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return "<redacted>";
            }

            var atIndex = email.IndexOf('@');
            if (atIndex <= 0)
            {
                // Not a standard email format, avoid logging raw value.
                return "<redacted>";
            }

            var localPart = email.Substring(0, atIndex);
            var domainPart = email.Substring(atIndex); // includes '@'

            if (localPart.Length <= 1)
            {
                return "***" + domainPart;
            }

            return localPart[0] + "***" + domainPart;
        }
        private readonly IConfiguration _configuration;
        private readonly ICloudflareJwtValidator _jwtValidator;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly ILogger<CloudflareAccessMiddleware> _logger;
        private readonly string _adminGroupClaimType;
        private readonly HashSet<string> _adminGroups;

        public CloudflareAccessMiddleware(
            RequestDelegate next,
            IConfiguration configuration,
            ICloudflareJwtValidator jwtValidator,
            IHostEnvironment hostEnvironment,
            ILogger<CloudflareAccessMiddleware>? logger = null)
        {
            _next = next;
            _configuration = configuration;
            _jwtValidator = jwtValidator;
            _hostEnvironment = hostEnvironment;
            _logger = logger ?? NullLogger<CloudflareAccessMiddleware>.Instance;
            _adminGroupClaimType = _configuration["CloudflareAccess:AdminGroupClaimType"] ?? "groups";
            _adminGroups = LoadConfiguredValues(_configuration, "CloudflareAccess:AdminGroups");
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var devSettings = _configuration.GetSection("DevSettings");
            if (_hostEnvironment.IsDevelopment() && devSettings.Exists() && devSettings.GetValue<bool>("SimulateCloudflare"))
            {
                var mockEmail = devSettings.GetValue<string>("MockEmail");
                if (!string.IsNullOrEmpty(mockEmail))
                {
                    var mockGroups = LoadConfiguredValues(_configuration, "DevSettings:MockGroups");
                    context.User = BuildPrincipal(mockEmail, mockEmail.Split('@')[0], "dev-mock-user", mockGroups, _adminGroupClaimType, _adminGroups);
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
                            LoadHeaderValues(context.Request.Headers["Cf-Access-Mock-Groups"].FirstOrDefault()),
                            _adminGroupClaimType,
                            _adminGroups
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
            var groups = ExtractGroupValues(validation.Principal, _adminGroupClaimType);
            var incomingClaimTypes = validation.Principal.Claims
                .Select(claim => claim.Type)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            context.User = BuildPrincipal(email, name, subject, groups, _adminGroupClaimType, _adminGroups);

            var redactedEmail = RedactEmail(email);

            _logger.LogDebug(
                "Cloudflare auth resolved Email={Email}, Subject={Subject}, IncomingClaimTypes={IncomingClaimTypes}, ExtractedGroups={ExtractedGroups}, ConfiguredAdminGroups={ConfiguredAdminGroups}, IsAdmin={IsAdmin}",
                redactedEmail,
                subject ?? string.Empty,
                incomingClaimTypes,
                groups,
                _adminGroups.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray(),
                context.User.IsInRole("Admin"));

            await _next(context);
        }

        private static ClaimsPrincipal BuildPrincipal(
            string email,
            string? name,
            string? userId,
            IEnumerable<string> groups,
            string adminGroupClaimType,
            IReadOnlySet<string> adminGroups)
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

            var resolvedGroups = groups
                .Where(group => !string.IsNullOrWhiteSpace(group))
                .Select(group => group.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var group in resolvedGroups)
            {
                claims.Add(new Claim(adminGroupClaimType, group));
            }

            if (resolvedGroups.Any(group => adminGroups.Contains(group)))
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            return new ClaimsPrincipal(new ClaimsIdentity(claims, "Cloudflare"));
        }

        private static HashSet<string> LoadConfiguredValues(IConfiguration configuration, string sectionPath)
        {
            var section = configuration.GetSection(sectionPath);
            var children = section.GetChildren()
                .Select(child => child.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim());

            var scalar = section.Value?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                ?? [];

            return children
                .Concat(scalar)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static IReadOnlyList<string> LoadHeaderValues(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return [];
            }

            return rawValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        private static IReadOnlyList<string> ExtractGroupValues(ClaimsPrincipal principal, string configuredClaimType)
        {
            var claimTypes = new[]
            {
                configuredClaimType,
                "groups",
                "group",
                ClaimTypes.GroupSid
            }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

            var claims = new List<string>();
            foreach (var claimType in claimTypes)
            {
                claims.AddRange(ExtractClaimValues(principal, claimType));
            }

            return claims
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static IReadOnlyList<string> ExtractClaimValues(ClaimsPrincipal principal, string claimType)
        {
            var claims = principal.FindAll(claimType)
                .SelectMany(claim => ExpandClaimValue(claim.Value))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return claims;
        }

        private static IEnumerable<string> ExpandClaimValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return [];
            }

            var trimmed = value.Trim();
            if (trimmed.StartsWith("[", StringComparison.Ordinal))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<string[]>(trimmed);
                    if (parsed is not null)
                    {
                        return parsed;
                    }
                }
                catch (JsonException)
                {
                    // Intentionally ignore JSON parse failures and fall back to treating the claim
                    // value as a comma-separated string or single value below. This avoids breaking
                    // authorization when claims are not strictly JSON array-shaped.
                }
            }

            if (trimmed.Contains(',', StringComparison.Ordinal))
            {
                return trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }

            return [trimmed];
        }
    }
}

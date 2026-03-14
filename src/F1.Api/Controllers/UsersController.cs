using F1.Core.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace F1.Api.Controllers;

[ApiController]
[Route("users")]
public class UsersController : ControllerBase
{
    private const int MaxDebugClaimValueLength = 128;
    private const int MaxAdditionalDebugClaims = 25;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;

    public UsersController(IConfiguration configuration, IHostEnvironment hostEnvironment)
    {
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
    }

    [HttpGet("me")]
    public ActionResult<UserDto> GetMe()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var name = User.FindFirstValue(ClaimTypes.Name);
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(name))
        {
            return Unauthorized();
        }

        return Ok(new UserDto
        {
            Name = name ?? string.Empty,
            Email = email ?? string.Empty,
            IsAuthenticated = true,
            IsAdmin = User.IsInRole("Admin"),
            Id = id ?? string.Empty
        });
    }

    [HttpGet("debug/me")]
    public ActionResult<UserDebugDto> GetDebugMe()
    {
        if (!IsDebugEndpointEnabled())
        {
            return NotFound();
        }

        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }

        var email = User.FindFirstValue(ClaimTypes.Email);
        var name = User.FindFirstValue(ClaimTypes.Name);
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(name))
        {
            return Unauthorized();
        }

        var adminGroupClaimType = _configuration["CloudflareAccess:AdminGroupClaimType"] ?? "groups";
        var configuredAdminGroups = LoadConfiguredValues("CloudflareAccess:AdminGroups");
        var allowedClaimTypes = GetAllowedClaimTypes(adminGroupClaimType);
        var discoveredClaimTypes = User.Claims
            .Select(claim => claim.Type)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var claims = BuildSanitizedDebugClaims(adminGroupClaimType);
        var additionalClaims = BuildAdditionalSanitizedClaims(allowedClaimTypes);

        var claimTypesToInspect = new[]
        {
            adminGroupClaimType,
            "groups",
            "group",
            ClaimTypes.GroupSid
        }
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

        var groups = User.Claims
            .Where(claim => claimTypesToInspect.Contains(claim.Type, StringComparer.OrdinalIgnoreCase))
            .Select(claim => TruncateValue(claim.Value, MaxDebugClaimValueLength))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var roles = User.FindAll(ClaimTypes.Role)
            .Select(claim => TruncateValue(claim.Value, MaxDebugClaimValueLength))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Ok(new UserDebugDto
        {
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
            IsAdmin = User.IsInRole("Admin"),
            AuthenticationType = User.Identity?.AuthenticationType ?? string.Empty,
            Email = RedactEmail(email),
            Name = string.Empty,
            Id = RedactIdentifier(id),
            AdminGroupClaimType = adminGroupClaimType,
            ConfiguredAdminGroups = configuredAdminGroups,
            Roles = roles,
            Groups = groups,
            DiscoveredClaimTypes = discoveredClaimTypes,
            Claims = claims,
            AdditionalClaims = additionalClaims
        });
    }

    private bool IsDebugEndpointEnabled()
    {
        return (_hostEnvironment.IsDevelopment() || _hostEnvironment.IsEnvironment("Test"))
               && _configuration.GetValue<bool>("DevSettings:EnableDebugEndpoints");
    }

    private static bool IsEmailClaim(string claimType)
    {
        return string.Equals(claimType, ClaimTypes.Email, StringComparison.OrdinalIgnoreCase)
               || string.Equals(claimType, "email", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsIdentifierClaim(string claimType)
    {
        return string.Equals(claimType, ClaimTypes.NameIdentifier, StringComparison.OrdinalIgnoreCase)
               || string.Equals(claimType, "sub", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSensitiveClaimType(string claimType)
    {
        if (string.IsNullOrWhiteSpace(claimType))
        {
            return false;
        }

        return claimType.Contains("token", StringComparison.OrdinalIgnoreCase)
               || claimType.Contains("secret", StringComparison.OrdinalIgnoreCase)
               || claimType.Contains("jwt", StringComparison.OrdinalIgnoreCase)
               || claimType.Contains("assertion", StringComparison.OrdinalIgnoreCase)
               || claimType.Contains("cookie", StringComparison.OrdinalIgnoreCase)
               || claimType.Contains("authorization", StringComparison.OrdinalIgnoreCase)
               || claimType.Contains("client-id", StringComparison.OrdinalIgnoreCase)
               || claimType.Contains("client-secret", StringComparison.OrdinalIgnoreCase)
               || claimType.Contains("sensitive", StringComparison.OrdinalIgnoreCase);
    }

    private static HashSet<string> GetAllowedClaimTypes(string adminGroupClaimType)
    {
        return new[]
        {
            ClaimTypes.Email,
            "email",
            ClaimTypes.NameIdentifier,
            "sub",
            ClaimTypes.Role,
            adminGroupClaimType,
            "groups",
            "group",
            ClaimTypes.GroupSid
        }
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private List<UserDebugClaimDto> BuildSanitizedDebugClaims(string adminGroupClaimType)
    {
        var allowedClaimTypes = GetAllowedClaimTypes(adminGroupClaimType);

        return User.Claims
            .Where(claim => allowedClaimTypes.Contains(claim.Type))
            .Select(claim => new UserDebugClaimDto
            {
                Type = claim.Type,
                Value = SanitizeClaimValue(claim.Type, claim.Value)
            })
            .ToList();
    }

    private List<UserDebugClaimDto> BuildAdditionalSanitizedClaims(IReadOnlySet<string> allowedClaimTypes)
    {
        return User.Claims
            .Where(claim => !allowedClaimTypes.Contains(claim.Type))
            .Select(claim => new UserDebugClaimDto
            {
                Type = claim.Type,
                Value = IsSensitiveClaimType(claim.Type)
                    ? "[redacted]"
                    : TruncateValue(claim.Value, MaxDebugClaimValueLength)
            })
            .Take(MaxAdditionalDebugClaims)
            .ToList();
    }

    private static string SanitizeClaimValue(string claimType, string? value)
    {
        if (IsEmailClaim(claimType))
        {
            return RedactEmail(value);
        }

        if (IsIdentifierClaim(claimType))
        {
            return RedactIdentifier(value);
        }

        return TruncateValue(value, MaxDebugClaimValueLength);
    }

    private static string RedactIdentifier(string? identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return string.Empty;
        }

        var trimmed = identifier.Trim();
        if (trimmed.Length <= 4)
        {
            return "***";
        }

        return $"{trimmed[..2]}***{trimmed[^2..]}";
    }

    private static string TruncateValue(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        if (trimmed.Length <= maxLength)
        {
            return trimmed;
        }

        return trimmed[..maxLength] + "...(truncated)";
    }

    private static string RedactEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return string.Empty;
        }

        var atIndex = email.LastIndexOf('@');
        var domain = atIndex >= 0 && atIndex < email.Length - 1
            ? email.Substring(atIndex + 1)
            : string.Empty;

        return string.IsNullOrEmpty(domain) ? "***" : $"***@{domain}";
    }

    private List<string> LoadConfiguredValues(string sectionPath)
    {
        var section = _configuration.GetSection(sectionPath);
        var children = section.GetChildren()
            .Select(child => child.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim());

        var scalar = section.Value?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? [];

        return children
            .Concat(scalar)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

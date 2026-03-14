using F1.Core.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace F1.Api.Controllers;

[ApiController]
[Route("users")]
public class UsersController : ControllerBase
{
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
        var claims = User.Claims
            .Select(claim => new UserDebugClaimDto
            {
                Type = claim.Type,
                Value = IsEmailClaim(claim.Type) ? RedactEmail(claim.Value) : claim.Value
            })
            .ToList();

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
            .Select(claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var roles = User.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Ok(new UserDebugDto
        {
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
            IsAdmin = User.IsInRole("Admin"),
            AuthenticationType = User.Identity?.AuthenticationType ?? string.Empty,
            Email = RedactEmail(email),
            Name = name ?? string.Empty,
            Id = id ?? string.Empty,
            AdminGroupClaimType = adminGroupClaimType,
            ConfiguredAdminGroups = configuredAdminGroups,
            Roles = roles,
            Groups = groups,
            Claims = claims
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

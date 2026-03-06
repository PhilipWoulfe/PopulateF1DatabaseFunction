using F1.Core.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;


namespace F1.Api.Controllers;

[ApiController]
[Route("user")]
public class UsersController : ControllerBase
{
    [HttpGet("me")]
    public ActionResult<UserDto> GetMe()
    {
        var jwtAssertion = Request.Headers["Cf-Access-Jwt-Assertion"].FirstOrDefault();
        var nameFromJwt = GetJwtClaim(jwtAssertion, "name");
        var emailFromJwt = GetJwtClaim(jwtAssertion, "email");

        var email = emailFromJwt ?? Request.Headers["Cf-Access-Authenticated-User-Email"].FirstOrDefault();
        var name = nameFromJwt ?? email?.Split('@')[0];

        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(name))
        {
            return Unauthorized();
        }

        return Ok(new UserDto
        {
            Name = name ?? string.Empty,
            Email = email ?? string.Empty,
            IsAuthenticated = true,
            Id = Request.Headers["Cf-Access-Authenticated-User-Id"].FirstOrDefault() ?? string.Empty
        });
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

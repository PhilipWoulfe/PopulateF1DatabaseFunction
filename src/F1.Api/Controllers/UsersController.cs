using F1.Core.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace F1.Api.Controllers;

[ApiController]
[Route("users")]
public class UsersController : ControllerBase
{
    [HttpGet("me")]
    public ActionResult<UserDto> GetMe()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var name = User.FindFirstValue(ClaimTypes.Name);
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Request.Headers["Cf-Access-Authenticated-User-Id"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(name))
        {
            return Unauthorized();
        }

        return Ok(new UserDto
        {
            Name = name ?? string.Empty,
            Email = email ?? string.Empty,
            IsAuthenticated = true,
            Id = id ?? string.Empty
        });
    }
}

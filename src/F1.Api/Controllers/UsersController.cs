using F1.Core.Dtos;
using Microsoft.AspNetCore.Mvc;


namespace F1.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet("me")] // Final: /users/me (when accessed directly or via Nginx stripping /api/)
    public ActionResult<UserDto> GetMe()
    {
        var email = Request.Headers["Cf-Access-Authenticated-User-Email"].FirstOrDefault();

        if (string.IsNullOrEmpty(email))
        {
            return Unauthorized();
        }

        return Ok(new UserDto
        {
            Name = email.Split('@')[0],
            Email = email,
            IsAuthenticated = true,
            Id = Request.Headers["Cf-Access-Authenticated-User-Id"].FirstOrDefault() ?? string.Empty
        });
    }

}

using F1.Core.Dtos;
using F1.Core.Interfaces;
using F1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Security.Claims;

namespace F1.Api.Controllers;

[ApiController]
[Route("selections")]
public class SelectionsController : ControllerBase
{
    private readonly ISelectionService _selectionService;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;

    public SelectionsController(
        ISelectionService selectionService,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment)
    {
        _selectionService = selectionService;
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
    }

    [HttpGet("{raceId}/config")]
    public IActionResult GetConfig(string raceId)
    {
        var config = _selectionService.GetRaceConfig(raceId);
        if (config is null)
        {
            return NotFound();
        }

        return Ok(config);
    }

    [HttpGet("{raceId}/mine")]
    public async Task<IActionResult> GetMine(string raceId)
    {
        var userId = ResolveUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var selection = await _selectionService.GetSelectionAsync(raceId, userId);
        if (selection is null)
        {
            return NotFound();
        }

        return Ok(selection);
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent()
    {
        var userId = ResolveUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (ShouldUseMockCurrentSelections())
        {
            return Ok(BuildMockCurrentSelections(userId));
        }

        var selections = await _selectionService.GetCurrentSelectionsAsync(userId);
        return Ok(selections);
    }

    [HttpPut("{raceId}/mine")]
    public async Task<IActionResult> UpsertMine(string raceId, [FromBody] SelectionSubmissionDto submission)
    {
        var userId = ResolveUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            var selection = await _selectionService.UpsertSelectionAsync(raceId, userId, submission);
            return Ok(selection);
        }
        catch (SelectionValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (SelectionForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    private string? ResolveUserId()
    {
        return User.FindFirstValue(ClaimTypes.Email)
               ?? Request.Headers["Cf-Access-Authenticated-User-Email"].FirstOrDefault();
    }

    private bool ShouldUseMockCurrentSelections()
    {
        return _hostEnvironment.IsDevelopment()
               && _configuration.GetValue<bool>("DevSettings:MockCurrentSelections");
    }

    private static IReadOnlyList<CurrentSelectionDto> BuildMockCurrentSelections(string userId)
    {
        var timestamp = new DateTime(2026, 3, 6, 9, 0, 0, DateTimeKind.Utc);
        var userName = userId.Split('@')[0];

        return
        [
            new CurrentSelectionDto
            {
                UserId = userId,
                UserName = userName,
                DriverId = "max_verstappen",
                DriverName = "Max Verstappen",
                SelectionType = "Regular",
                Timestamp = timestamp
            },
            new CurrentSelectionDto
            {
                UserId = userId,
                UserName = userName,
                DriverId = "lando_norris",
                DriverName = "Lando Norris",
                SelectionType = "Regular",
                Timestamp = timestamp.AddMinutes(1)
            },
            new CurrentSelectionDto
            {
                UserId = userId,
                UserName = userName,
                DriverId = "charles_leclerc",
                DriverName = "Charles Leclerc",
                SelectionType = "Regular",
                Timestamp = timestamp.AddMinutes(2)
            },
            new CurrentSelectionDto
            {
                UserId = userId,
                UserName = userName,
                DriverId = "oscar_piastri",
                DriverName = "Oscar Piastri",
                SelectionType = "Regular",
                Timestamp = timestamp.AddMinutes(3)
            },
            new CurrentSelectionDto
            {
                UserId = userId,
                UserName = userName,
                DriverId = "lewis_hamilton",
                DriverName = "Lewis Hamilton",
                SelectionType = "Regular",
                Timestamp = timestamp.AddMinutes(4)
            }
        ];
    }
}

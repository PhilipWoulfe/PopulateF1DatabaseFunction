using F1.Core.Dtos;
using F1.Core.Interfaces;
using F1.Core.Models;
using F1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace F1.Api.Controllers;

[ApiController]
[Route("selections")]
public class SelectionsController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, Selection> MockSelections = new(StringComparer.OrdinalIgnoreCase);
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

        if (ShouldUseMockCurrentSelections())
        {
            return Ok(GetOrCreateMockSelection(raceId, userId));
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
            var selection = GetOrCreateMockSelection(SelectionService.AustraliaRaceId2026, userId);
            return Ok(MapCurrentSelections(selection));
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

        if (ShouldUseMockCurrentSelections())
        {
            var validationMessage = ValidateMockSubmission(submission);
            if (validationMessage is not null)
            {
                return BadRequest(new { message = validationMessage });
            }

            var selection = UpsertMockSelection(raceId, userId, submission);
            return Ok(selection);
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

    private static Selection GetOrCreateMockSelection(string raceId, string userId)
    {
        var key = BuildMockSelectionKey(raceId, userId);
        return MockSelections.GetOrAdd(key, _ => BuildDefaultMockSelection(raceId, userId));
    }

    private static Selection UpsertMockSelection(string raceId, string userId, SelectionSubmissionDto submission)
    {
        var orderedSelections = submission.OrderedSelections;
        var key = BuildMockSelectionKey(raceId, userId);
        var updated = new Selection
        {
            Id = MockSelections.TryGetValue(key, out var existing) ? existing.Id : Guid.NewGuid(),
            RaceId = raceId,
            UserId = userId,
            OrderedSelections = orderedSelections,
            BetType = submission.BetType,
            SubmittedAtUtc = DateTime.UtcNow,
            IsLocked = false
        };

        MockSelections[key] = updated;
        return updated;
    }

    private static Selection BuildDefaultMockSelection(string raceId, string userId)
    {
        return new Selection
        {
            Id = Guid.NewGuid(),
            RaceId = raceId,
            UserId = userId,
            BetType = BetType.Regular,
            SubmittedAtUtc = new DateTime(2026, 3, 6, 9, 0, 0, DateTimeKind.Utc),
            IsLocked = false,
            OrderedSelections =
            [
                new SelectionPosition { Position = 1, DriverId = "max_verstappen" },
                new SelectionPosition { Position = 2, DriverId = "lando_norris" },
                new SelectionPosition { Position = 3, DriverId = "charles_leclerc" },
                new SelectionPosition { Position = 4, DriverId = "oscar_piastri" },
                new SelectionPosition { Position = 5, DriverId = "lewis_hamilton" }
            ]
        };
    }

    private static IReadOnlyList<CurrentSelectionDto> MapCurrentSelections(Selection selection)
    {
        var userName = selection.UserId.Split('@')[0];
        var orderedSelections = selection.OrderedSelections;
        var rows = new List<CurrentSelectionDto>(orderedSelections.Count);

        foreach (var selectionItem in orderedSelections)
        {
            var driverId = selectionItem.DriverId;
            if (string.IsNullOrWhiteSpace(driverId))
            {
                continue;
            }

            rows.Add(new CurrentSelectionDto
            {
                Position = selectionItem.Position,
                UserId = selection.UserId,
                UserName = userName,
                DriverId = driverId,
                DriverName = ResolveMockDriverName(driverId),
                SelectionType = selection.BetType.ToString(),
                Timestamp = selection.SubmittedAtUtc
            });
        }

        return rows;
    }

    private static string ResolveMockDriverName(string driverId)
    {
        return driverId switch
        {
            "max_verstappen" => "Max Verstappen",
            "lando_norris" => "Lando Norris",
            "charles_leclerc" => "Charles Leclerc",
            "oscar_piastri" => "Oscar Piastri",
            "lewis_hamilton" => "Lewis Hamilton",
            "leclerc" => "Charles Leclerc",
            "norris" => "Lando Norris",
            "hamilton" => "Lewis Hamilton",
            "piastri" => "Oscar Piastri",
            _ => driverId
        };
    }

    private static string? ValidateMockSubmission(SelectionSubmissionDto submission)
    {
        var validSelections = submission.OrderedSelections
            .Where(item => !string.IsNullOrWhiteSpace(item.DriverId))
            .ToList();

        var distinctPositions = validSelections
            .Select(item => item.Position)
            .Distinct()
            .Count();

        var distinctCount = submission.OrderedSelections
            .Where(item => !string.IsNullOrWhiteSpace(item.DriverId))
            .Select(item => item.DriverId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var totalCount = submission.OrderedSelections.Count;
        if (totalCount != 5 || validSelections.Count != 5 || distinctCount != 5 || distinctPositions != 5)
        {
            return "Exactly 5 unique drivers must be selected.";
        }

        if (validSelections.Any(item => item.Position < 1 || item.Position > 5))
        {
            return "Selection positions must be between 1 and 5.";
        }

        return null;
    }

    private static string BuildMockSelectionKey(string raceId, string userId)
    {
        return $"{raceId}::{userId}";
    }
}

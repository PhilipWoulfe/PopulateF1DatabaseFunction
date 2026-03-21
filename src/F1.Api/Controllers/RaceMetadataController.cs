using F1.Core.Dtos;
using F1.Core.Exceptions;
using F1.Core.Interfaces;
using F1.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;

namespace F1.Api.Controllers;

[ApiController]
[Route("races/{raceId}/metadata")]
public class RaceMetadataController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, RaceQuestionMetadata> MockMetadata = new(StringComparer.OrdinalIgnoreCase);

    private readonly IRaceMetadataService _raceMetadataService;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RaceMetadataController(
        IRaceMetadataService raceMetadataService,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        IDateTimeProvider dateTimeProvider)
    {
        _raceMetadataService = raceMetadataService;
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _dateTimeProvider = dateTimeProvider;
    }

    [HttpGet]
    public async Task<IActionResult> GetMetadata(string raceId, [FromQuery] bool includeDraft = false)
    {
        if (includeDraft && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        if (ShouldUseMockRaceMetadata())
        {
            var mock = GetOrCreateMockMetadata(raceId);
            if (!includeDraft && !mock.IsPublished)
            {
                return NotFound();
            }

            return Ok(MapToDto(mock));
        }

        var metadata = await _raceMetadataService.GetMetadataAsync(raceId, publishedOnly: !includeDraft);
        if (metadata is null)
        {
            return NotFound();
        }

        return Ok(MapToDto(metadata));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut]
    public async Task<IActionResult> UpsertMetadata(string raceId, [FromBody] UpsertRaceQuestionMetadataDto request)
    {
        if (ShouldUseMockRaceMetadata())
        {
            var mock = new RaceQuestionMetadata
            {
                RaceId = raceId,
                H2HQuestion = request.H2HQuestion,
                BonusQuestion = request.BonusQuestion,
                IsPublished = request.IsPublished,
                UpdatedAtUtc = _dateTimeProvider.UtcNow,
                ETag = Guid.NewGuid().ToString("N")
            };

            MockMetadata[raceId] = mock;
            return Ok(MapToDto(mock));
        }

        try
        {
            var metadata = await _raceMetadataService.UpsertMetadataAsync(
                raceId,
                new RaceQuestionMetadata
                {
                    H2HQuestion = request.H2HQuestion,
                    BonusQuestion = request.BonusQuestion,
                    IsPublished = request.IsPublished
                });

            return Ok(MapToDto(metadata));
        }
        catch (MetadataValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private bool ShouldUseMockRaceMetadata()
    {
        return _hostEnvironment.IsDevelopment()
               && _configuration.GetValue<bool>("DevSettings:MockRaceMetadata");
    }

    private RaceQuestionMetadata GetOrCreateMockMetadata(string raceId)
    {
        return MockMetadata.GetOrAdd(raceId, _ => new RaceQuestionMetadata
        {
            RaceId = raceId,
            H2HQuestion = "Who finishes higher: Leclerc or Norris?",
            BonusQuestion = "How many safety-car laps will there be?",
            IsPublished = true,
            UpdatedAtUtc = _dateTimeProvider.UtcNow,
            ETag = Guid.NewGuid().ToString("N")
        });
    }

    private static RaceQuestionMetadataDto MapToDto(RaceQuestionMetadata metadata)
    {
        return new RaceQuestionMetadataDto
        {
            RaceId = metadata.RaceId,
            H2HQuestion = metadata.H2HQuestion,
            BonusQuestion = metadata.BonusQuestion,
            IsPublished = metadata.IsPublished,
            UpdatedAtUtc = metadata.UpdatedAtUtc,
            ETag = metadata.ETag
        };
    }
}
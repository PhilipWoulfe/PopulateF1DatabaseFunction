namespace F1.Core.Dtos;

public class RaceQuestionMetadataDto
{
    public string RaceId { get; set; } = string.Empty;

    public string H2HQuestion { get; set; } = string.Empty;

    public string BonusQuestion { get; set; } = string.Empty;

    public bool IsPublished { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public string? ETag { get; set; }
}
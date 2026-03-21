namespace F1.Core.Dtos;

public class UpsertRaceQuestionMetadataDto
{
    public string H2HQuestion { get; set; } = string.Empty;

    public string BonusQuestion { get; set; } = string.Empty;

    public bool IsPublished { get; set; }
}
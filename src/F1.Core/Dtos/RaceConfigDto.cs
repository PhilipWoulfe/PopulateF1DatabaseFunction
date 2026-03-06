namespace F1.Core.Dtos;

public class RaceConfigDto
{
    public string RaceId { get; set; } = string.Empty;
    public DateTime PreQualyDeadlineUtc { get; set; }
    public DateTime FinalDeadlineUtc { get; set; }
}

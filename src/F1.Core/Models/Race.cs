namespace F1.Core.Models;

public class Race
{
    public string Id { get; set; } = string.Empty;
    public int CompetitionId { get; set; }
    public int Season { get; set; }
    public int Round { get; set; }
    public string RaceName { get; set; } = string.Empty;
    public string CircuitName { get; set; } = string.Empty;
    public DateTime StartTimeUtc { get; set; }
    public DateTime PreQualyDeadlineUtc { get; set; }
    public DateTime FinalDeadlineUtc { get; set; }
}

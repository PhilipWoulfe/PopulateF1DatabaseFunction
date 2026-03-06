namespace F1.Web.Models;

public class RaceConfig
{
    public string RaceId { get; set; } = string.Empty;
    public DateTime PreQualyDeadlineUtc { get; set; }
    public DateTime FinalDeadlineUtc { get; set; }
}

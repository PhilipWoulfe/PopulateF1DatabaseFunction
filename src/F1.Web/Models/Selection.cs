namespace F1.Web.Models;

public class Selection
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string RaceId { get; set; } = string.Empty;
    public List<string> Selections { get; set; } = [];
    public BetType BetType { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public bool IsLocked { get; set; }
}

public class SelectionSubmission
{
    public List<string> Selections { get; set; } = [];
    public BetType BetType { get; set; }
}

namespace F1.Web.Models;

public class Selection
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string RaceId { get; set; } = string.Empty;
    public List<SelectionPosition> OrderedSelections { get; set; } = [];
    // Legacy flat list retained temporarily for backward compatibility.
    public List<string> Selections { get; set; } = [];
    public BetType BetType { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public bool IsLocked { get; set; }
}

public class SelectionSubmission
{
    public List<SelectionPosition> OrderedSelections { get; set; } = [];
    // Legacy flat list retained temporarily for backward compatibility.
    public List<string> Selections { get; set; } = [];
    public BetType BetType { get; set; }
}

public class SelectionPosition
{
    public int Position { get; set; }
    public string DriverId { get; set; } = string.Empty;
}

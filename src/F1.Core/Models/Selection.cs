using Newtonsoft.Json;

namespace F1.Core.Models;

public class Selection
{
    [JsonProperty("id")]
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public string RaceId { get; set; } = string.Empty;
    public List<SelectionPosition> OrderedSelections { get; set; } = [];    
    public BetType BetType { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public bool IsLocked { get; set; }
}

public class SelectionPosition
{
    public int Position { get; set; }
    public string DriverId { get; set; } = string.Empty;
}

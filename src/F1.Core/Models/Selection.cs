using Newtonsoft.Json;

namespace F1.Core.Models;

public class Selection
{
    [JsonProperty("id")]
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public string RaceId { get; set; } = string.Empty;
    public List<string> Selections { get; set; } = [];
    public BetType BetType { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public bool IsLocked { get; set; }
}

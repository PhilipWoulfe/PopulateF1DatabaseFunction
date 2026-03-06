using Newtonsoft.Json;

namespace F1.Core.Models;

public class Driver
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string DriverId { get; set; } = string.Empty;
    public int? PermanentNumber { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
}

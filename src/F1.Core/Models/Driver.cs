using Newtonsoft.Json;

namespace F1.Core.Models
{
    public class Driver
    {
        [JsonProperty("id")]
        public string? Id { get; set; }
        public string? FullName { get; set; }
        public string? DriverId { get; set; }
        public int? PermanentNumber { get; set; }
        public string? Code { get; set; }
        public string? Nationality { get; set; }
    }
}

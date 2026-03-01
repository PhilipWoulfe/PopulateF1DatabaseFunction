using Newtonsoft.Json;
using PopulateF1Database.Models.Interfaces;

namespace PopulateF1Database.Models
{
    public class RaceWithResults : Race, IIdentifiable
    {
        //
        // Summary:
        //     Gets the list of race results.
        [JsonProperty(nameof(Results))]
        public IList<RaceResult> Results { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string Id => $"{Season}-{Round:D2}-{Circuit.CircuitId}";

        public DateTime LastModified => DateTime.UtcNow;
    }
}

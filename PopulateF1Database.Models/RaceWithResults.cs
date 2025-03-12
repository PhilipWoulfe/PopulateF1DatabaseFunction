using JolpicaResult = JolpicaApi.Responses.Models.RaceInfo.RaceWithResults;
using PopulateF1Database.Models.Interfaces;
using Newtonsoft.Json;

namespace PopulateF1Database.Models
{
    public class RaceWithResults : JolpicaResult, IIdentifiable 
    {
        [JsonProperty(PropertyName = "id")]
        public required string Id { get; set; }
    }
}

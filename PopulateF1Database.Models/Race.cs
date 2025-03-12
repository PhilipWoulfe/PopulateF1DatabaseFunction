using JolpicaRace = JolpicaApi.Responses.Models.RaceInfo.Race;
using PopulateF1Database.Models.Interfaces;
using Newtonsoft.Json;

namespace PopulateF1Database.Models
{
    public class Race : JolpicaRace, IIdentifiable 
    {
        [JsonProperty(PropertyName = "id")]
        public required string Id { get; set; }
    }
}

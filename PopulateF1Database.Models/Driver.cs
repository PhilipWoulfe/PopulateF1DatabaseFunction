using JolpicaDriver = JolpicaApi.Responses.Models.Driver;
using PopulateF1Database.Models.Interfaces;
using Newtonsoft.Json;

namespace PopulateF1Database.Models
{
    public class Driver : JolpicaDriver, IIdentifiable 
    {
        [JsonProperty(PropertyName = "id")]
        public required string Id { get; set; }
    }
}

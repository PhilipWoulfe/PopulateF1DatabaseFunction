using Newtonsoft.Json;
using PopulateF1Database.Models.Interfaces;
using JolpicaDriver = JolpicaApi.Responses.Models.Driver;

namespace PopulateF1Database.Models
{
    public class Driver : JolpicaDriver, IIdentifiable
    {
        [JsonProperty(PropertyName = "id")]
        public string Id => DriverId;

        public DateTime LastModified => DateTime.UtcNow;
    }
}

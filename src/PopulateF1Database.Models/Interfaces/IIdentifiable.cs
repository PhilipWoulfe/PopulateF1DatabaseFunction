using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace PopulateF1Database.Models.Interfaces
{
    public interface IIdentifiable
    {
        [JsonProperty("id")]
        [JsonPropertyName("id")]
        string Id { get; }


        [JsonProperty("lastModified")]
        [JsonPropertyName("lastModified")]
        DateTime LastModified { get; }
    }
}

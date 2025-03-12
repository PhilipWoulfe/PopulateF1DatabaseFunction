using System.Text.Json.Serialization;

namespace PopulateF1Database.Models.Interfaces
{
    public interface IIdentifiable
    {
        [JsonPropertyName("id")]
        string Id { get; set; }
    }
}

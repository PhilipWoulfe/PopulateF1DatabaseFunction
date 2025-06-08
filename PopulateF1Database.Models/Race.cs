using JolpicaApi.Responses.Models;
using Newtonsoft.Json;

namespace PopulateF1Database.Models
{
    public class Race
    {
        //
        // Summary:
        //     Gets the season of the race.
        [JsonProperty("season")]
        public int Season { get; set; }

        //
        // Summary:
        //     Gets the round of the race.
        [JsonProperty("round")]
        public int Round { get; set; }

        //
        // Summary:
        //     Gets the name of the race.
        [JsonProperty("raceName")]
        public string RaceName { get; set; }

        //
        // Summary:
        //     Gets the Wikipedia URL of the race.
        [JsonProperty("url")]
        public string WikiUrl { get; set; }

        //
        // Summary:
        //     Gets the circuit details of the race.
        [JsonProperty("circuit")]
        public Circuit Circuit { get; set; }

        //
        // Summary:
        //     Gets the start time of the race.
        public DateTime StartTime
        {
            get; set;
        }

        //
        // Summary:
        //     Gets the raw date of the race.
        [JsonProperty("date")]
        public string DateRaw { get; set; }

        //
        // Summary:
        //     Gets the raw time of the race.
        [JsonProperty("time")]
        public string TimeRaw { get; set; }
    }
}

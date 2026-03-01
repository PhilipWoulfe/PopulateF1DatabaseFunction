using JolpicaApi.Responses.Models;
using Newtonsoft.Json;

namespace PopulateF1Database.Models
{
    public class RaceResult
    {
        // Summary:
        //     Gets the number of the result.
        [JsonProperty("number")]
        public int Number { get; set; }

        //
        // Summary:
        //     Gets the position of the result.
        [JsonProperty("position")]
        public int Position { get; set; }

        //
        // Summary:
        //     Gets the driver associated with the result.
        [JsonProperty(nameof(Driver))]
        public Driver Driver { get; set; }

        //
        // Summary:
        //     Gets the constructor associated with the result.
        [JsonProperty(nameof(Constructor))]
        public Constructor Constructor { get; set; }

        //
        // Summary:
        //     Finishing position. R = Retired, D = Disqualified, E = Excluded, W = Withdrawn,
        //     F = Failed to qualify, N = Not classified. See JolpicaApi.Responses.Models.RaceInfo.RaceResult.StatusText
        //     for more info.
        [JsonProperty("positionText")]
        public string PositionText { get; set; }

        //
        // Summary:
        //     Indicates if the driver retired from the race.
        public bool Retired
        {
            get; set;

        }

        //
        // Summary:
        //     Indicates if the driver was disqualified from the race.
        public bool Disqualified => PositionText == "D";

        //
        // Summary:
        //     Indicates if the driver was classified (not retired and finished 90% of the race).
        public bool Classified
        {
            get; set;
        }

        //
        // Summary:
        //     Points awarded to the driver for the race.
        [JsonProperty("points")]
        public double Points { get; set; }

        //
        // Summary:
        //     Grid position, i.e. starting position. A value of 0 means the driver started
        //     from the pit lane.
        [JsonProperty("grid")]
        public int Grid { get; set; }

        //
        // Summary:
        //     Indicates if the driver started from the pit lane.
        public bool StartedFromPitLane => Grid == 0;

        //
        // Summary:
        //     Number of laps completed by the driver.
        [JsonProperty("laps")]
        public int Laps { get; set; }

        //
        // Summary:
        //     Status text describing the finishing status of the driver.
        [JsonProperty("status")]
        public string StatusText { get; set; }
    }
}

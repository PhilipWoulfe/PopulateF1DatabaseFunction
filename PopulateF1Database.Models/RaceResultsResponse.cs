using JolpicaApi.Serialization;

namespace PopulateF1Database.Models
{
    public class RaceResultsResponse : JolpicaApi.Responses.RaceInfo.RaceResultsResponse
    {
        /// <summary>
        /// Gets the list of drivers.
        /// </summary>
        [JsonPathProperty("DriverTable.Drivers")]
        public new IList<RaceWithResults> Races { get; private set; }
    }
}

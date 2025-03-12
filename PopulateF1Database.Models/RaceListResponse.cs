using JolpicaApi.Serialization;

namespace PopulateF1Database.Models
{
    public class RaceListResponse : JolpicaApi.Responses.RaceInfo.RaceListResponse
    {
        /// <summary>
        /// Gets the list of drivers.
        /// </summary>
        [JsonPathProperty("DriverTable.Drivers")]
        public new IList<Race> Races { get; private set; }
    }
}

using Newtonsoft.Json;
using JolpicaDriversResponse = JolpicaApi.Responses.DriverResponse;

namespace PopulateF1Database.Models
{
    public class DriverResponse : JolpicaDriversResponse
    {
        /// <summary>
        /// Gets the list of drivers.
        /// </summary>
        [JsonProperty("DriverTable.Drivers")]
        public new IList<Driver> Drivers { get; private set; }
    }
}

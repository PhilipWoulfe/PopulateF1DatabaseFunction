using JolpicaApi.Serialization;
using JolpicaDriversResponse = JolpicaApi.Responses.DriverResponse;

namespace PopulateF1Database.Models
{
    public class DriverResponse : JolpicaDriversResponse
    {
        /// <summary>
        /// Gets the list of drivers.
        /// </summary>
        [JsonPathProperty("DriverTable.Drivers")]
        public new IList<Driver> Drivers { get; private set; }
    }
}

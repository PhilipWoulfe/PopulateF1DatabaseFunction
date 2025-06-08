using Newtonsoft.Json;


namespace PopulateF1Database.Models
{
    public class RaceResultsResponse
    {
        /// <summary>  
        /// Gets the list of drivers.  
        /// </summary>  
        [JsonProperty("DriverTable.Drivers")] // Replace JsonPathProperty with JsonProperty from Newtonsoft.Json  
        public IList<RaceWithResults> Races { get; set; }
    }
}

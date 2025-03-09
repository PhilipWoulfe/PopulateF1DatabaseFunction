using JolpicaApi.Client;
using JolpicaApi.Ids;
using JolpicaApi.Requests;
using JolpicaApi.Requests.Standard;
using JolpicaApi.Responses.RaceInfo;
using Microsoft.Extensions.Options;
using PopulateF1Database.Config;
using PopulateF1Database.Services.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;


namespace PopulateF1Database.Services.Services
{
    public class JolpicaService(IJolpicaClient jolpicaClient) : IJolpicaService
    {
        private readonly IJolpicaClient _jolpicaClient = jolpicaClient;

        public async Task<RaceResultsResponse> GetDataAsync()
        {
            // All request properties are optional (except 'Season' if 'Round' is set)
            var request = new RaceResultsRequest
            {
                Season = "2017",     // or Seasons.Current for current season
                Round = "11",        // or Rounds.Last or Rounds.Next for last or next round
                DriverId = "vettel", // or Drivers.SebastianVettel
                Limit = 30,      // Limit the number of results returned
                Offset = 0      // Result offset (used for paging)
            };

            // RaceResultsRequest returns a RaceResultsResponse
            // Other requests returns other response types
            RaceResultsResponse response = await _jolpicaClient.GetResponseAsync(request);

            return response;
        }
    }
}

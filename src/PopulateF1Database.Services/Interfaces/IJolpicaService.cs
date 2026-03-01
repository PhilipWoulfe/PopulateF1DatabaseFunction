using JolpicaApi.Responses;
using JolpicaApi.Responses.RaceInfo;

namespace PopulateF1Database.Services.Interfaces
{
    public interface IJolpicaService
    {
        Task<RaceListResponse> GetRounds();

        Task<RaceResultsResponse> GetResults(string round);

        Task<DriverResponse> GetDrivers();
    }
}

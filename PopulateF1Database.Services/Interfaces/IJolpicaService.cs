using JolpicaApi.Responses.RaceInfo;

namespace PopulateF1Database.Services.Interfaces
{
    public interface IJolpicaService
    {
        Task<RaceResultsResponse> GetDataAsync();
    }
}

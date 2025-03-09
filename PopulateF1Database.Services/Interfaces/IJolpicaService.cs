using JolpicaApi.Responses.RaceInfo;
using System.Threading.Tasks;

namespace PopulateF1Database.Services.Interfaces
{
    public interface IJolpicaService
    {
        Task<RaceResultsResponse> GetDataAsync();
    }
}

//using JolpicaApi.Responses.RaceInfo;

using PopulateF1Database.Models;

namespace PopulateF1Database.DataAccess.Interfaces
{
    public interface IRaceRepository
    {
        Task WriteRacesAsync(RaceListResponse driverResponse);
    }
}
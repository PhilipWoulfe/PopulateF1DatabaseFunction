using JolpicaApi.Responses.RaceInfo;

namespace PopulateF1Database.DataAccess.Interfaces
{
    public interface IRaceRepository
    {
        Task WriteRacesAsync(RaceListResponse driverResponse);
    }
}
//using JolpicaApi.Responses.RaceInfo;

using PopulateF1Database.Models;

namespace PopulateF1Database.DataAccess.Interfaces
{
    public interface IResultsRepository
    {
        Task WriteResultsAsync(RaceResultsResponse driverResponse);
    }
}
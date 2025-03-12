//using JolpicaApi.Responses;

using PopulateF1Database.Models;

namespace PopulateF1Database.DataAccess.Interfaces
{
    public interface IDriverRepository
    {
        Task WriteDriversAsync(DriverResponse driverResponse);
    }
}
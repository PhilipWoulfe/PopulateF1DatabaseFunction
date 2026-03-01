using JolpicaApi.Responses;
using PopulateF1Database.Services.Interfaces;

namespace PopulateF1Database.Services.Drivers.QueryHandlers
{
    public class GetDriversQueryHandler(IJolpicaService jolpicaService)
    {
        public async Task<DriverResponse> Handle()
        {
            return await jolpicaService.GetDrivers();
        }
    }
}
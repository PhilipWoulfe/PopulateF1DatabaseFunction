using JolpicaApi.Responses;

namespace PopulateF1Database.Services.Drivers.CommandHandlers.Commands
{
    public class WriteDriversCommand
    {
        public required DriverResponse DriverResponse { get; set; }
    }
}
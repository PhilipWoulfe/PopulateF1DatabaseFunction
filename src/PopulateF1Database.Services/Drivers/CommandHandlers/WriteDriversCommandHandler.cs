using AutoMapper;
using PopulateF1Database.DataAccess.Interfaces;
using PopulateF1Database.Models;
using PopulateF1Database.Services.Drivers.CommandHandlers.Commands;
using PopulateF1Database.Services.Interfaces;

namespace PopulateF1Database.Services.Drivers.CommandHandlers
{
    public class WriteDriversCommandHandler(IDriverRepository dataRepository, IMapper mapper) : IWriteDriversCommandHandler
    {
        public async Task Handle(WriteDriversCommand command)
        {
            var driverResponse = mapper.Map<DriverResponse>(command.DriverResponse);
            await dataRepository.WriteDriversAsync(driverResponse);
        }
    }
}
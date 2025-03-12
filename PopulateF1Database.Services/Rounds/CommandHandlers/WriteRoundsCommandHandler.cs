using AutoMapper;
using PopulateF1Database.DataAccess.Interfaces;
using PopulateF1Database.Models;
using PopulateF1Database.Services.Interfaces;
using PopulateF1Database.Services.Results.CommandHandlers.Commands;
using PopulateF1Database.Services.Rounds.CommandHandlers.Commands;

namespace PopulateF1Database.Services.Rounds.CommandHandlers
{
    public class WriteRoundsCommandHandler(IRaceRepository dataRepository, IMapper mapper) : IWriteRoundsCommandHandler
    {
        public async Task Handle(WriteRoundsCommand command)
        {
            var raceResponse = mapper.Map<RaceListResponse>(command.RaceListResponse);
            await dataRepository.WriteRacesAsync(raceResponse);
        }
    }
}

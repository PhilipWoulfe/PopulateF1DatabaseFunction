using AutoMapper;
using PopulateF1Database.DataAccess.Interfaces;
using PopulateF1Database.Models;
using PopulateF1Database.Services.Interfaces;
using PopulateF1Database.Services.Results.CommandHandlers.Commands;

namespace PopulateF1Database.Services.Results.CommandHandlers
{
    public class WriteResultsCommandHandler(IResultsRepository dataRepository, IMapper mapper) : IWriteResultsCommandHandler
    {
        public async Task Handle(WriteResultsCommand command)
        {
            var resultsResponse = mapper.Map<RaceResultsResponse>(command.RaceResults);

            await dataRepository.WriteResultsAsync(resultsResponse);
        }
    }
}
using JolpicaApi.Responses.RaceInfo;

namespace PopulateF1Database.Services.Results.CommandHandlers.Commands
{
    public class WriteResultsCommand
    {
        public required RaceResultsResponse RaceResultsResponse { get; set; }
    }
}
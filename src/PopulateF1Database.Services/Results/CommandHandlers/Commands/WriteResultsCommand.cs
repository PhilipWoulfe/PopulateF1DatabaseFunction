using JolpicaApi.Responses.Models.RaceInfo;

namespace PopulateF1Database.Services.Results.CommandHandlers.Commands
{
    public class WriteResultsCommand
    {
        public required Dictionary<Race, IList<RaceResult>> RaceResults { get; set; }
    }
}
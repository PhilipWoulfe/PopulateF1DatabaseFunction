using JolpicaApi.Responses.RaceInfo;

namespace PopulateF1Database.Services.Rounds.CommandHandlers.Commands
{
    public class WriteRoundsCommand
    {
        public required RaceListResponse RaceListResponse { get; set; }
    }
}
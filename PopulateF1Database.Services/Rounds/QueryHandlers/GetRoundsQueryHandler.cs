using JolpicaApi.Responses.RaceInfo;
using PopulateF1Database.Services.Interfaces;

namespace PopulateF1Database.Services.Rounds.QueryHandlers
{
    public class GetRoundsQueryHandler(IJolpicaService jolpicaService)
    {
        public async Task<RaceListResponse> Handle()
        {
            return await jolpicaService.GetRounds();
        }
    }
}
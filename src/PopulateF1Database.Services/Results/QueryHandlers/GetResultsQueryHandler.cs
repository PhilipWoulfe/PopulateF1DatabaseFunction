using JolpicaApi.Responses.RaceInfo;
using PopulateF1Database.Services.Interfaces;

namespace PopulateF1Database.Services.Results.QueryHandlers
{
    public class GetResultsQueryHandler(IJolpicaService jolpicaService)
    {
        public async Task<RaceResultsResponse> Handle(string round)
        {
            return await jolpicaService.GetResults(round);
        }
    }
}
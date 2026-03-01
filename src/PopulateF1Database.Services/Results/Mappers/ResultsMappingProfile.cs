using AutoMapper;
using JolpicaRaceResult = JolpicaApi.Responses.Models.RaceInfo.RaceResult;
using Race = JolpicaApi.Responses.Models.RaceInfo.Race;
using RaceResult = PopulateF1Database.Models.RaceResult;
using RaceResultsResponse = PopulateF1Database.Models.RaceResultsResponse;
using RaceWithResults = PopulateF1Database.Models.RaceWithResults;

namespace PopulateF1Database.Services.Results.Mappers
{
    public class ResultsMappingProfile : Profile
    {
        public ResultsMappingProfile()
        {
            CreateMap<JolpicaRaceResult, RaceResult>();

            CreateMap<Race, RaceWithResults>();

            CreateMap<Dictionary<Race, IList<JolpicaRaceResult>>, RaceResultsResponse>()
                .ForMember(dest => dest.Races, opt => opt.MapFrom((src, dest, destMember, context) => src.Select(kvp =>
                {
                    var raceWithResults = context.Mapper.Map<RaceWithResults>(kvp.Key);
                    raceWithResults.Results = [.. kvp.Value.Select(result => context.Mapper.Map<RaceResult>(result))];
                    return raceWithResults;
                }).ToList()));
        }
    }
}
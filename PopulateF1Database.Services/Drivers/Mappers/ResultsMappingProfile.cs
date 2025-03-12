using AutoMapper;
using PopulateF1Database.Models;
using JolpicaRaceWithResults = JolpicaApi.Responses.Models.RaceInfo.RaceWithResults;
using JolpicaResultsResponse = JolpicaApi.Responses.RaceInfo.RaceResultsResponse;

namespace PopulateF1Database.Services.Drivers.Mappers
{
    public class ResultsMappingProfile : Profile
    {
        public ResultsMappingProfile()
        {
            CreateMap<JolpicaRaceWithResults, RaceWithResults>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid().ToString()));

            CreateMap<JolpicaResultsResponse, RaceResultsResponse>();
        }
    }
}

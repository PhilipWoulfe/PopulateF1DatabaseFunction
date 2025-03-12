using AutoMapper;
using PopulateF1Database.Models;
using JolpicaRace = JolpicaApi.Responses.Models.RaceInfo.Race;

namespace PopulateF1Database.Services.Drivers.Mappers
{
    public class RaceMappingProfile : Profile
    {
        public RaceMappingProfile()
        {
            CreateMap<JolpicaRace, Race>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid().ToString()));

            CreateMap<JolpicaApi.Responses.RaceInfo.RaceResponse<JolpicaRace>, RaceListResponse>();
        }
    }
}

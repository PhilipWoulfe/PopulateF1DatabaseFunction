using AutoMapper;
using PopulateF1Database.Models;
using JolpicaDriver = JolpicaApi.Responses.Models.Driver;
using JolpicaDriverResponse = JolpicaApi.Responses.DriverResponse;

namespace PopulateF1Database.Services.Drivers.Mappers
{
    public class DriverMappingProfile : Profile
    {
        public DriverMappingProfile()
        {
            CreateMap<JolpicaDriver, Driver>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid().ToString()));                                

            CreateMap<JolpicaDriverResponse, DriverResponse>();
        }
    }
}

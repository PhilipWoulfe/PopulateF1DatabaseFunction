using AutoMapper;
using PopulateF1Database.Models;
using PopulateF1Database.Services.Results.Mappers;
using JolpicaRaceResult = JolpicaApi.Responses.Models.RaceInfo.RaceResult;
using Race = JolpicaApi.Responses.Models.RaceInfo.Race;

namespace PopulateF1Database.Tests.Services.Results.Mappers
{
    public class ResultsMappingProfileTests
    {
        private readonly IMapper _mapper;

        public ResultsMappingProfileTests()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<ResultsMappingProfile>());
            _mapper = config.CreateMapper();
        }

        [Fact]
        public void Should_Map_JolpicaRaceResult_To_RaceResult()
        {
            var source = new JolpicaRaceResult
            {
                // Example properties, adjust as per your model
                Position = 1,
                Number = 44,
            };

            var result = _mapper.Map<RaceResult>(source);

            Assert.NotNull(result);
            Assert.Equal(source.Position, result.Position);
            Assert.Equal(source.Number, result.Number);
        }

        [Fact]
        public void Should_Map_Race_To_RaceWithResults()
        {
            var source = new Race
            {
                RaceName = "Australian Grand Prix",
                Round = 1,
            };

            var result = _mapper.Map<RaceWithResults>(source);

            Assert.NotNull(result);
            Assert.Equal(source.RaceName, result.RaceName);
            Assert.Equal(source.Round, result.Round);
        }

        [Fact]
        public void Should_Map_Dictionary_To_RaceResultsResponse()
        {
            var race = new Race
            {
                RaceName = "Australian Grand Prix",
                Round = 1,
            };
            var raceResult = new JolpicaRaceResult
            {
                Position = 1,
                Number = 44,
            };
            var dict = new Dictionary<Race, IList<JolpicaRaceResult>>
            {
                { race, new List<JolpicaRaceResult> { raceResult } }
            };

            var result = _mapper.Map<RaceResultsResponse>(dict);

            Assert.NotNull(result);
            Assert.NotNull(result.Races);
            Assert.Single(result.Races);
            Assert.NotNull(result.Races[0].Results);
            Assert.Single(result.Races[0].Results);
            Assert.Equal(raceResult.Number, result.Races[0].Results[0].Number);
        }
    }
}
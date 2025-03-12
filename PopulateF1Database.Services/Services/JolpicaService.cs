using JolpicaApi.Client;
using JolpicaApi.Requests.Standard;
using JolpicaApi.Responses.RaceInfo;
using PopulateF1Database.Config;
using PopulateF1Database.Services.Interfaces;
using Microsoft.Extensions.Logging;

using AutoMapper;
using JolpicaApi.Responses;

namespace PopulateF1Database.Services.Services
{
    public class JolpicaService(IJolpicaClient jolpicaClient,
            AppConfig appConfig,
            ILogger<JolpicaService> logger,
            IMapper mapper) : IJolpicaService
    {
        private readonly IJolpicaClient _jolpicaClient = jolpicaClient;
        private readonly AppConfig _appConfig = appConfig;
        private readonly ILogger<JolpicaService> _logger = logger;
        private readonly IMapper _mapper = mapper;

        public async Task<DriverResponse> GetDrivers()
        {
            var request = new DriverInfoRequest
            {
                Season = _appConfig.CompetitionYear
            };

            var apiResponse = await ExecuteApiRequest(async () => await _jolpicaClient.GetResponseAsync(request));
            return _mapper.Map<DriverResponse>(apiResponse);
        }

        public async Task<RaceResultsResponse> GetResults(string round)
        {
            var request = new RaceResultsRequest
            {
                Season = _appConfig.CompetitionYear,
                Round = round
            };

            return await ExecuteApiRequest(async () => await _jolpicaClient.GetResponseAsync(request));
        }

        public async Task<RaceListResponse> GetRounds()
        {
            var request = new RaceListRequest
            {
                Season = _appConfig.CompetitionYear
            };

            return await ExecuteApiRequest(async () => await _jolpicaClient.GetResponseAsync(request));
        }

        private async Task<T> ExecuteApiRequest<T>(Func<Task<T>> apiRequest)
        {
            try
            {
                return await apiRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while executing the API request.");
                throw;
            }
        }
    }
}

using JolpicaApi.Responses.Models.RaceInfo;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PopulateF1Database.Config;
using PopulateF1Database.Services.Drivers.CommandHandlers.Commands;
using PopulateF1Database.Services.Interfaces;
using PopulateF1Database.Services.Results.CommandHandlers.Commands;

namespace PopulateF1Database.Functions
{
    public class UpdateDatabase(
        ILogger<UpdateDatabase> logger,
        IWriteDriversCommandHandler driverCommandHandler,
        IWriteResultsCommandHandler resultsCommandHandler,
        IJolpicaService jolpicaService,
        AppConfig config)
    {

        [Function("UpdateDatabase")]
        public async Task Run([TimerTrigger("%UpdateDatabaseCronSchedule%")] TimerInfo myTimer)
        {
            logger.LogInformation("C# Timer trigger function executed at: {time}", DateTime.Now);

            if (myTimer.ScheduleStatus is not null)
            {
                logger.LogInformation("Next timer schedule at: {nextSchedule}", myTimer.ScheduleStatus.Next);
            }

            try
            {
                // Fetch and write data concurrently

                var tasks = new List<Task>
                {
                    FetchAndWriteDriversAsync(),
                    FetchAndWriteRacesAndResultsAsync()
                };

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while updating the database.");
                throw;
            }
        }

        private async Task FetchAndWriteDriversAsync()
        {
            try
            {
                var driverResponse = await jolpicaService.GetDrivers();
                await driverCommandHandler.Handle(new WriteDriversCommand { DriverResponse = driverResponse });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while fetching and writing drivers.");
                throw;
            }
        }

        private async Task FetchAndWriteRacesAndResultsAsync()
        {
            try
            {
                var raceListResponse = await jolpicaService.GetRounds();
                var raceResultsMap = new Dictionary<Race, IList<RaceResult>>();

                foreach (var race in raceListResponse.Races)
                {
                    try
                    {
                        IList<RaceResult> raceResults = [];

                        if (race.StartTime < DateTime.UtcNow)
                        {
                            // Introduce a delay to avoid hitting rate limits
                            await Task.Delay(config.JolpicaRateLimitDelayMs); // Use the rate limit delay from config
                            var raceResultsResponse = await jolpicaService.GetResults(race.Round.ToString());
                            var results = raceResultsResponse.Races.FirstOrDefault()?.Results;

                            if (results != null)
                            {
                                foreach (var result in results)
                                {
                                    raceResults.Add(result);
                                }
                            }
                        }

                        raceResultsMap[race] = raceResults;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "An error occurred while fetching and writing results for race: {Round}", race.Round);
                        throw;
                    }
                }

                await resultsCommandHandler.Handle(new WriteResultsCommand { RaceResults = raceResultsMap });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while fetching and writing races and results.");
                throw;
            }
        }
    }
}
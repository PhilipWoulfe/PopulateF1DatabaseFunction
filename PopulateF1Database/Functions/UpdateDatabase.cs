using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PopulateF1Database.DataAccess.Interfaces;
using PopulateF1Database.Services.Drivers.CommandHandlers;
using PopulateF1Database.Services.Drivers.CommandHandlers.Commands;
using PopulateF1Database.Services.Interfaces;
using PopulateF1Database.Services.Results.CommandHandlers;
using PopulateF1Database.Services.Results.CommandHandlers.Commands;
using PopulateF1Database.Services.Rounds.CommandHandlers;
using PopulateF1Database.Services.Rounds.CommandHandlers.Commands;

namespace PopulateF1Database.Functions
{
    public class UpdateDatabase(
        ILogger<UpdateDatabase> logger,
        IWriteDriversCommandHandler driverCommandHandler,
        IWriteRoundsCommandHandler raceCommandHandler,
        IWriteResultsCommandHandler resultsCommandHandler,
        IJolpicaService jolpicaService)
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
                //var v = await driverRepository.ReadPreSeasonQuestionsAsync();
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
                await raceCommandHandler.Handle(new WriteRoundsCommand { RaceListResponse = raceListResponse });

                var resultTasks = raceListResponse.Races.Select(async race =>
                {
                    try
                    {
                        var raceResultsResponse = await jolpicaService.GetResults(race.Round.ToString());
                        await resultsCommandHandler.Handle(new WriteResultsCommand { RaceResultsResponse = raceResultsResponse });
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "An error occurred while fetching and writing results for race: {Round}", race.Round);
                        throw;
                    }
                });

                await Task.WhenAll(resultTasks);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while fetching and writing races and results.");
                throw;
            }
        }
    }
}
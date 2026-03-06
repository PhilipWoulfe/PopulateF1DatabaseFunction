using Microsoft.Extensions.Logging;
using PopulateF1Database.DataAccess.Interfaces;
using PopulateF1Database.Models;

namespace PopulateF1Database.DataAccess.Repositories
{
    public class DriverRepository(ICosmoDataRepository cosmosDataRepository,
        ILogger<DriverRepository> logger) : IDriverRepository
    {
        public async Task WriteDriversAsync(DriverResponse driverResponse)
        {
            try
            {
                if (driverResponse is null)
                {
                    logger.LogError("driverResponse is null in WriteDriversAsync.");
                    throw new ArgumentNullException(nameof(driverResponse));
                }

                if (driverResponse.Drivers is null)
                {
                    logger.LogError("driverResponse.Drivers is null in WriteDriversAsync.");
                    throw new InvalidOperationException("Driver response contains a null Drivers collection.");
                }

                await cosmosDataRepository.UpsertItemsAsync(driverResponse.Drivers);
            }
            catch (AggregateException ex)
            {
                foreach (var error in ex.InnerExceptions)
                {
                    logger.LogError(error, "An error occurred while writing drivers.");
                }
                throw new AggregateException("One or more errors occurred while writing drivers.", ex.InnerExceptions);
            }
        }
    }
}

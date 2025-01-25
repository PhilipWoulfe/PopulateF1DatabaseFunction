using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace PopulateF1Database
{
    public class UpdateDatabase
    {
        private readonly ILogger _logger;

        public UpdateDatabase(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UpdateDatabase>();
        }

        [Function("UpdateDatabase")]
        public void Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation("C# Timer trigger function executed at: {time}", DateTime.Now);

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation("Next timer schedule at: {nextSchedule}", myTimer.ScheduleStatus.Next);
            }
        }
    }
}

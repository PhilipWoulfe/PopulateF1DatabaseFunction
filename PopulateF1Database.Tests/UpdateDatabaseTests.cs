using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PopulateF1Database.Config;
using PopulateF1Database.Functions;
using Xunit;

namespace PopulateF1Database.Tests
{
    public class UpdateDatabaseTests
    {
        [Fact]
        public void Run_LogsExecutionTime()
        {
            // Arrange
            var loggerFactoryMock = new Mock<ILoggerFactory>();
            var loggerMock = new Mock<ILogger<UpdateDatabase>>();
            loggerFactoryMock.Setup(lf => lf.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);

            var config = Options.Create(new AppConfig
            {
                AzureWebJobsStorage = "UseDevelopmentStorage=true",
                UpdateDatabaseCronSchedule = "0 */5 * * * *",
                CosmosDBConnectionString = "your-cosmos-db-connection-string",
                JolpicaApi = new JolpicaApiConfig
                {
                    BaseUrl = "https://api.jolpi.ca/ergast/f1/"
                }
            });
            var function = new UpdateDatabase(loggerFactoryMock.Object, config);

            var scheduleStatus = new ScheduleStatus
            {
                Last = DateTime.Now.AddMinutes(-5),
                Next = DateTime.Now.AddMinutes(5)
            };
            var timerInfoMock = new Mock<TimerInfo>(MockBehavior.Strict, new ScheduleStatus(), scheduleStatus, false);

            // Act
            function.Run(timerInfoMock.Object);

            // Assert
            loggerMock.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("C# Timer trigger function executed at") == true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public void Run_LogsNextSchedule()
        {
            // Arrange
            var loggerFactoryMock = new Mock<ILoggerFactory>();
            var loggerMock = new Mock<ILogger<UpdateDatabase>>();
            loggerFactoryMock.Setup(lf => lf.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);

            var config = Options.Create(new AppConfig
            {
                AzureWebJobsStorage = "UseDevelopmentStorage=true",
                UpdateDatabaseCronSchedule = "0 */5 * * * *",
                CosmosDBConnectionString = "your-cosmos-db-connection-string",
                JolpicaApi = new JolpicaApiConfig
                {
                    BaseUrl = "https://api.jolpi.ca/ergast/f1/"
                }
            });
            var function = new UpdateDatabase(loggerFactoryMock.Object, config);

            var scheduleStatus = new ScheduleStatus
            {
                Last = DateTime.Now.AddMinutes(-5),
                Next = DateTime.Now.AddMinutes(5)
            };
            var timerInfoMock = new Mock<TimerInfo>(MockBehavior.Strict, new ScheduleStatus(), scheduleStatus, false);

            // Act
            function.Run(timerInfoMock.Object);

            // Assert
            loggerMock.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Next timer schedule at") == true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }
    }
}

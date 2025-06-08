//using JolpicaApi.Responses.RaceInfo;
//using Microsoft.Azure.Functions.Worker;
//using Microsoft.Extensions.Logging;
//using Moq;
//using PopulateF1Database.DataAccess.Interfaces;
//using PopulateF1Database.Functions;
//using PopulateF1Database.Services.Interfaces;
//using PopulateF1Database.Tests.Mocks;
//using JolpicaApi.Responses.Models.RaceInfo;
//using PopulateF1Database.Models;
//using Race = JolpicaApi.Responses.Models.RaceInfo.Race;
//using PopulateF1Database.Services.Results.CommandHandlers;
//using PopulateF1Database.Services.Drivers.CommandHandlers;
//using PopulateF1Database.Services.Rounds.CommandHandlers;
//using PopulateF1Database.Services.Drivers.CommandHandlers.Commands;
//using PopulateF1Database.Services.Rounds.CommandHandlers.Commands;
//using PopulateF1Database.Services.Results.CommandHandlers.Commands;

//namespace PopulateF1Database.Tests.Functions
//{
//    public class UpdateDatabaseTests
//    {
//        private readonly Mock<ILogger<UpdateDatabase>> loggerMock;
//        private readonly Mock<WriteDriversCommandHandler> driverCommandHandlerMock;
//        private readonly Mock<WriteRoundsCommandHandler> raceCommandHandlerMock;
//        private readonly Mock<WriteResultsCommandHandler> resultsCommandHandlerMock;
//        private readonly Mock<IJolpicaService> jolpicaServiceMock;
//        private readonly UpdateDatabase function;
//        private readonly Mock<TimerInfo> timerInfoMock;

//        public UpdateDatabaseTests()
//        {
//            loggerMock = MockDependencies.CreateLoggerMock<UpdateDatabase>();
//            driverCommandHandlerMock = new Mock<WriteDriversCommandHandler>();
//            raceCommandHandlerMock = new Mock<WriteRoundsCommandHandler>();
//            resultsCommandHandlerMock = new Mock<WriteResultsCommandHandler>();
//            jolpicaServiceMock = MockDependencies.CreateJolpicaServiceMock();

//            function = new UpdateDatabase(
//                loggerMock.Object,
//                driverCommandHandlerMock.Object,
//                raceCommandHandlerMock.Object,
//                resultsCommandHandlerMock.Object,
//                jolpicaServiceMock.Object);

//            var scheduleStatus = new ScheduleStatus
//            {
//                Last = DateTime.Now.AddMinutes(-5),
//                Next = DateTime.Now.AddMinutes(5)
//            };
//            timerInfoMock = new Mock<TimerInfo>(MockBehavior.Strict, new ScheduleStatus(), scheduleStatus, false);
//        }

//        [Fact]
//        public async Task Run_LogsExecutionTime()
//        {
//            // Act
//            await function.Run(timerInfoMock.Object);

//            // Assert
//            loggerMock.Verify(
//                x => x.Log(
//                    It.Is<LogLevel>(l => l == LogLevel.Information),
//                    It.IsAny<EventId>(),
//                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("C# Timer trigger function executed at") == true),
//                    It.IsAny<Exception>(),
//                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
//                Times.Once);
//        }

//        [Fact]
//        public async Task Run_LogsNextSchedule()
//        {
//            // Act
//            await function.Run(timerInfoMock.Object);

//            // Assert
//            loggerMock.Verify(
//                x => x.Log(
//                    It.Is<LogLevel>(l => l == LogLevel.Information),
//                    It.IsAny<EventId>(),
//                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Next timer schedule at") == true),
//                    It.IsAny<Exception>(),
//                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
//                Times.Once);
//        }

//        [Fact]
//        public async Task Run_FetchesAndWritesData()
//        {
//            // Mock the JolpicaService responses
//            jolpicaServiceMock.Setup(s => s.GetDrivers()).ReturnsAsync(new DriverResponse());
//            jolpicaServiceMock.Setup(s => s.GetRounds()).ReturnsAsync(new JolpicaApi.Responses.RaceInfo.RaceListResponse());
//            jolpicaServiceMock.Setup(s => s.GetResults(It.IsAny<string>())).ReturnsAsync(new JolpicaApi.Responses.RaceInfo.RaceResultsResponse());

//            // Act
//            await function.Run(timerInfoMock.Object);

//            // Assert
//            driverCommandHandlerMock.Verify(r => r.Handle(It.IsAny<WriteDriversCommand>()), Times.Once);
//            raceCommandHandlerMock.Verify(r => r.Handle(It.IsAny<WriteRoundsCommand>()), Times.Once);
//            resultsCommandHandlerMock.Verify(r => r.Handle(It.IsAny<WriteResultsCommand>()), Times.AtLeastOnce);
//        }

//        [Fact]
//        public async Task Run_HandlesException()
//        {
//            // Mock the JolpicaService to throw an exception
//            jolpicaServiceMock.Setup(s => s.GetDrivers()).ThrowsAsync(new Exception("Test exception"));

//            // Act & Assert
//            await Assert.ThrowsAsync<Exception>(() => function.Run(timerInfoMock.Object));

//            loggerMock.Verify(
//                x => x.Log(
//                    It.Is<LogLevel>(l => l == LogLevel.Error),
//                    It.IsAny<EventId>(),
//                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("An error occurred while updating the database.") == true),
//                    It.IsAny<Exception>(),
//                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
//                Times.Once);
//        }

//        [Fact]
//        public async Task Run_PartialFailureInResults()
//        {
//            // Mock the JolpicaService responses
//            jolpicaServiceMock.Setup(s => s.GetDrivers()).ReturnsAsync(new DriverResponse());
//            jolpicaServiceMock.Setup(s => s.GetRounds()).ReturnsAsync(new JolpicaApi.Responses.RaceInfo.RaceListResponse
//            {
//                Races =
//                    [
//                        new Mock<Race>().SetupProperty(r => r.Round, 1).Object,
//                        new Mock<Race>().SetupProperty(r => r.Round, 2).Object
//                    ]
//            });

//            jolpicaServiceMock.Setup(s => s.GetResults("1")).ReturnsAsync(new JolpicaApi.Responses.RaceInfo.RaceResultsResponse());
//            jolpicaServiceMock.Setup(s => s.GetResults("2")).ThrowsAsync(new Exception("Test exception"));

//            // Act
//            await function.Run(timerInfoMock.Object);

//            // Assert
//            driverCommandHandlerMock.Verify(r => r.Handle(It.IsAny<WriteDriversCommand>()), Times.Once);
//            raceCommandHandlerMock.Verify(r => r.Handle(It.IsAny<WriteRoundsCommand>()), Times.Once);
//            resultsCommandHandlerMock.Verify(r => r.Handle(It.IsAny<WriteResultsCommand>()), Times.Once);
//            loggerMock.Verify(
//                x => x.Log(
//                    It.Is<LogLevel>(l => l == LogLevel.Error),
//                    It.IsAny<EventId>(),
//                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("An error occurred while fetching and writing results for race: 2") == true),
//                    It.IsAny<Exception>(),
//                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
//                Times.Once);
//        }
//    }
//}
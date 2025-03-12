using Microsoft.Extensions.Logging;
using Moq;
using PopulateF1Database.DataAccess.Interfaces;
using PopulateF1Database.DataAccess.Repositories;
using PopulateF1Database.Services.Interfaces;

namespace PopulateF1Database.Tests.Mocks
{
    public static class MockDependencies
    {
        public static Mock<ILogger<T>> CreateLoggerMock<T>()
        {
            return new Mock<ILogger<T>>();
        }

        public static Mock<IDriverRepository> CreateDriverRepositoryMock()
        {
            return new Mock<IDriverRepository>();
        }

        public static Mock<IRaceRepository> CreateRaceRepositoryMock()
        {
            return new Mock<IRaceRepository>();
        }

        public static Mock<IResultsRepository> CreateResultRepositoryMock()
        {
            return new Mock<IResultsRepository>();
        }

        public static Mock<IJolpicaService> CreateJolpicaServiceMock()
        {
            return new Mock<IJolpicaService>();
        }
    }
}
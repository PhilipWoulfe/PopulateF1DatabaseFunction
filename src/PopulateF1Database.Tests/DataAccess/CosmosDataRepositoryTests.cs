//using Moq;
//using Microsoft.Azure.Cosmos;
//using PopulateF1Database.Config;
//using PopulateF1Database.DataAccess.Repositories;
//using PopulateF1Database.Models;
//using Xunit;
//using FluentAssertions;
//using System.Threading.Tasks;
//using System.Collections.Generic;
//using Newtonsoft.Json;
//using System;
//using Microsoft.Azure.Cosmos.Serialization.HybridRow;
//using JolpicaRaceResult = JolpicaApi.Responses.Models.RaceInfo.RaceResult;
////using JolpicaApi.Responses.Models;

//namespace PopulateF1Database.Tests.DataAccess
//{
//    public class CosmosDataRepositoryTests
//    {
//        private readonly Mock<CosmosClient> _cosmosClientMock;
//        private readonly Mock<Container> _containerMock;
//        private readonly CosmosDataRepository _repository;
//        private readonly CosmoDbConfig _config;

//        public CosmosDataRepositoryTests()
//        {
//            _cosmosClientMock = new Mock<CosmosClient>();
//            _containerMock = new Mock<Container>();
//            _config = new CosmoDbConfig
//            {
//                CosmosDbConnectionString = "AccountEndpoint=https://your-account.documents.azure.com:443/;AccountKey=c2VjcmV0S2V5MTIzNDU2Nzg5MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTIzNDU2Nzg5MDEyMw==",
//                CosmosDbDatabaseId = "test-database",
//                Containers = new ContainersConfig
//                {
//                    DriversContainer = "drivers",
//                    RacesContainer = "races",
//                    ResultsContainer = "results",
//                    PreSeasonQuestionsContainer = "pre-season-questions",
//                    SprintsContainer = "sprints",
//                    UsersContainer = "users"
//                },
//                RetryCount = 3,
//                RetryTime = 5
//            };

//            _cosmosClientMock.Setup(client => client.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
//                .Returns(_containerMock.Object);

//            _repository = new CosmosDataRepository(_config);
//        }

//        [Fact]
//        public async Task UpsertItemAsync_ShouldCreateCorrectJson()
//        {
//            // Arrange
//            var mockDriver = new Mock<Driver>();
//            mockDriver.Setup(d => d.Id).Returns("1");
//            mockDriver.Setup(d => d.FirstName).Returns("Lewis");
//            mockDriver.Setup(d => d.LastName).Returns("Hamilton");

//            string capturedJson = null;

//            _containerMock.Setup(container => container.UpsertItemAsync(
//                It.IsAny<object>(),
//                It.IsAny<PartitionKey>(),
//                It.IsAny<ItemRequestOptions>(),
//                It.IsAny<CancellationToken>()))
//                .Callback<object, PartitionKey, ItemRequestOptions, CancellationToken>((item, pk, options, ct) =>
//                {
//                    capturedJson = JsonConvert.SerializeObject(item);
//                })
//                .ReturnsAsync(Mock.Of<ItemResponse<object>>());

//            // Act
//            await _repository.UpsertItemAsync(mockDriver.Object);

//            // Assert
//            capturedJson.Should().NotBeNull();
//            var deserializedDriver = JsonConvert.DeserializeObject<dynamic>(capturedJson);
//            deserializedDriver.FirstName.ToString().Should().Be("Lewis");
//            deserializedDriver.LastName.ToString().Should().Be("Hamilton");
//            deserializedDriver.Id.ToString().Should().Be("1");
//        }

//        [Fact]
//        public async Task UpsertItemAsync_ShouldGenerateIdIfNotProvided()
//        {
//            // Arrange
//            var mockDriver = new Mock<Driver>();
//            mockDriver.Setup(d => d.Id).Returns((string)null);
//            mockDriver.Setup(d => d.FirstName).Returns("Charles");
//            mockDriver.Setup(d => d.LastName).Returns("Leclerc");

//            string capturedJson = null;

//            _containerMock.Setup(container => container.UpsertItemAsync(
//                It.IsAny<object>(),
//                It.IsAny<PartitionKey>(),
//                It.IsAny<ItemRequestOptions>(),
//                It.IsAny<CancellationToken>()))
//                .Callback<object, PartitionKey, ItemRequestOptions, CancellationToken>((item, pk, options, ct) =>
//                {
//                    capturedJson = JsonConvert.SerializeObject(item);
//                })
//                .ReturnsAsync(Mock.Of<ItemResponse<object>>());

//            // Act
//            await _repository.UpsertItemAsync(mockDriver.Object);

//            // Assert
//            capturedJson.Should().NotBeNull();
//            var deserializedDriver = JsonConvert.DeserializeObject<dynamic>(capturedJson);
//            deserializedDriver.FirstName.ToString().Should().Be("Charles");
//            deserializedDriver.LastName.ToString().Should().Be("Leclerc");
//            deserializedDriver.Id.ToString().Should().NotBeNullOrEmpty();
//        }

//        [Fact]
//        public async Task UpsertItemsAsync_ShouldUpsertAllItems()
//        {
//            // Arrange
//            var drivers = new List<Driver>
//            {
//                Mock.Of<Driver>(d => d.Id == "1" && d.FirstName == "Lewis" && d.LastName == "Hamilton"),
//                Mock.Of<Driver>(d => d.Id == "2" && d.FirstName == "Max" && d.LastName == "Verstappen")
//            };

//            var upsertedItems = new List<object>();

//            _containerMock.Setup(container => container.UpsertItemAsync(
//                It.IsAny<object>(),
//                It.IsAny<PartitionKey>(),
//                It.IsAny<ItemRequestOptions>(),
//                It.IsAny<CancellationToken>()))
//                .Callback<object, PartitionKey, ItemRequestOptions, CancellationToken>((item, pk, options, ct) =>
//                {
//                    upsertedItems.Add(item);
//                })
//                .ReturnsAsync(Mock.Of<ItemResponse<object>>());

//            // Act
//            await _repository.UpsertItemsAsync(drivers);

//            // Assert
//            upsertedItems.Should().HaveCount(2);

//            var firstItemJson = JsonConvert.SerializeObject(upsertedItems[0]);
//            var secondItemJson = JsonConvert.SerializeObject(upsertedItems[1]);

//            var firstDriver = JsonConvert.DeserializeObject<dynamic>(firstItemJson);
//            var secondDriver = JsonConvert.DeserializeObject<dynamic>(secondItemJson);

//            firstDriver.FirstName.ToString().Should().Be("Lewis");
//            firstDriver.LastName.ToString().Should().Be("Hamilton");
//            firstDriver.Id.ToString().Should().Be("1");

//            secondDriver.FirstName.ToString().Should().Be("Max");
//            secondDriver.LastName.ToString().Should().Be("Verstappen");
//            secondDriver.Id.ToString().Should().Be("2");
//        }

//        [Fact]
//        public async Task Driver_ShouldSerializeWithCamelCaseProperties()
//        {
//            // Arrange
//            var driver = new Mock<Driver>();
//            driver.Setup(d => d.Id).Returns("driver-123");
//            driver.Setup(d => d.FirstName).Returns("Lewis");
//            driver.Setup(d => d.LastName).Returns("Hamilton");
//            driver.Setup(d => d.DateOfBirth).Returns(DateTime.TryParse("1985-01-07", out DateTime result) ? result : DateTime.Now);
//            driver.Setup(d => d.Nationality).Returns("British");
//            driver.Setup(d => d.DriverId).Returns("hamilton");
//            driver.Setup(d => d.Code).Returns("HAM");
//            driver.Setup(d => d.PermanentNumber).Returns(44);
//            driver.Setup(d => d.WikiUrl).Returns("https://example.com/hamilton");

//            _containerMock.Setup(container => container.UpsertItemAsync(
//                It.IsAny<object>(),
//                It.IsAny<PartitionKey>(),
//                It.IsAny<ItemRequestOptions>(),
//                It.IsAny<CancellationToken>()))
//                .ReturnsAsync(Mock.Of<ItemResponse<object>>());

//            string capturedJson = null;

//            _containerMock.Setup(container => container.UpsertItemAsync(
//                It.IsAny<object>(),
//                It.IsAny<PartitionKey>(),
//                It.IsAny<ItemRequestOptions>(),
//                It.IsAny<CancellationToken>()))
//                .Callback<object, PartitionKey, ItemRequestOptions, CancellationToken>((item, pk, options, ct) =>
//                {
//                    capturedJson = JsonConvert.SerializeObject(item);
//                })
//                .ReturnsAsync(Mock.Of<ItemResponse<object>>());

//            // Act
//            await _repository.UpsertItemAsync(driver.Object);

//            // Assert
//            capturedJson.Should().NotBeNull();

//            // Verify all properties are serialized with camelCase
//            capturedJson.Should().Contain("\"id\":\"driver-123\"");
//            capturedJson.Should().Contain("\"firstName\":\"Lewis\"");
//            capturedJson.Should().Contain("\"lastName\":\"Hamilton\"");
//            capturedJson.Should().Contain("\"dateOfBirth\":\"1985-01-07\"");
//            capturedJson.Should().Contain("\"nationality\":\"British\"");
//            capturedJson.Should().Contain("\"driverId\":\"hamilton\"");
//            capturedJson.Should().Contain("\"code\":\"HAM\"");
//            capturedJson.Should().Contain("\"permanentNumber\":\"44\"");
//            capturedJson.Should().Contain("\"url\":\"https://example.com/hamilton\"");
//        }

//        [Fact]
//        public async Task Race_ShouldSerializeWithCamelCaseProperties()
//        {
//            // Arrange
//            var race = new Mock<Race>();
//            race.Setup(r => r.Id).Returns("race-123");
//            race.Setup(r => r.Season).Returns(2023);
//            race.Setup(r => r.Round).Returns(1);
//            race.Setup(r => r.WikiUrl).Returns("https://example.com/race1");
//            race.Setup(r => r.RaceName).Returns("Australian Grand Prix");
//            race.Setup(r => r.StartTime).Returns(DateTime.Now);

//        string capturedJson = null;

//            _containerMock.Setup(container => container.UpsertItemAsync(
//                It.IsAny<object>(),
//                It.IsAny<PartitionKey>(),
//                It.IsAny<ItemRequestOptions>(),
//                It.IsAny<CancellationToken>()))
//                .Callback<object, PartitionKey, ItemRequestOptions, CancellationToken>((item, pk, options, ct) =>
//                {
//                    capturedJson = JsonConvert.SerializeObject(item);
//                })
//                .ReturnsAsync(Mock.Of<ItemResponse<object>>());

//            // Act
//            await _repository.UpsertItemAsync(race.Object);

//            // Assert
//            capturedJson.Should().NotBeNull();

//            // Verify all properties are serialized with camelCase
//            capturedJson.Should().Contain("\"id\":\"race-123\"");
//            capturedJson.Should().Contain("\"season\":\"2023\"");
//            capturedJson.Should().Contain("\"round\":1");
//            capturedJson.Should().Contain("\"url\":\"https://example.com/race1\"");
//            capturedJson.Should().Contain("\"raceName\":\"Australian Grand Prix\"");
//            capturedJson.Should().Contain("\"circuitId\":\"albert_park\"");
//            capturedJson.Should().Contain("\"date\":\"2023-03-12\"");
//            capturedJson.Should().Contain("\"time\":\"05:00:00Z\"");
//        }

//        [Fact]
//        public async Task RaceWithResults_ShouldSerializeWithCamelCaseProperties()
//        {
//            // Arrange
//            var mockDriver = new Mock<Driver>();
//            mockDriver.Setup(d => d.Id).Returns((string)null);
//            mockDriver.Setup(d => d.FirstName).Returns("Charles");
//            mockDriver.Setup(d => d.LastName).Returns("Leclerc");

//            // Arrange
//            var raceResult = new Mock<RaceWithResults>();
//            raceResult.Setup(r => r.Id).Returns("result-123");
//            raceResult.Setup(r => r.Season).Returns(2023);
//            raceResult.Setup(r => r.Round).Returns(1);
//            raceResult.Setup(r => r.RaceName).Returns("Australian Grand Prix");
//            //raceResult.Setup(r => r.).Returns(DateTime.Now);

//            var jolpicaRaceResult = new Mock<JolpicaRaceResult>();
//            jolpicaRaceResult.Setup(r => r.Position).Returns(1);
//            jolpicaRaceResult.Setup(r => r.PositionText).Returns("1");
//            jolpicaRaceResult.Setup(r => r.Points).Returns(25);
//            jolpicaRaceResult.Setup(r => r.Driver).Returns(mockDriver.Object);
//            jolpicaRaceResult.Setup(r => r.Status).Returns(JolpicaApi.Responses.Models.FinishingStatusId.Accident);

//            // Setup complex properties
//            var resultsList = new List<JolpicaRaceResult>
//            {
//                jolpicaRaceResult.Object
//            };

//            raceResult.Setup(r => r.Results).Returns(resultsList);

//            string capturedJson = null;

//            _containerMock.Setup(container => container.UpsertItemAsync(
//                It.IsAny<object>(),
//                It.IsAny<PartitionKey>(),
//                It.IsAny<ItemRequestOptions>(),
//                It.IsAny<CancellationToken>()))
//                .Callback<object, PartitionKey, ItemRequestOptions, CancellationToken>((item, pk, options, ct) =>
//                {
//                    capturedJson = JsonConvert.SerializeObject(item);
//                })
//                .ReturnsAsync(Mock.Of<ItemResponse<object>>());

//            // Act
//            await _repository.UpsertItemAsync(raceResult.Object);

//            // Assert
//            capturedJson.Should().NotBeNull();

//            // Verify all properties are serialized with camelCase
//            capturedJson.Should().Contain("\"id\":\"result-123\"");
//            capturedJson.Should().Contain("\"season\":\"2023\"");
//            capturedJson.Should().Contain("\"round\":1");
//            capturedJson.Should().Contain("\"raceName\":\"Australian Grand Prix\"");
//            capturedJson.Should().Contain("\"date\":\"2023-03-12\"");
//            capturedJson.Should().Contain("\"time\":\"05:00:00Z\"");

//            // Results array properties
//            capturedJson.Should().Contain("\"position\":\"1\"");
//            capturedJson.Should().Contain("\"positionText\":\"1\"");
//            capturedJson.Should().Contain("\"points\":\"25\"");
//            capturedJson.Should().Contain("\"driverId\":\"hamilton\"");
//            capturedJson.Should().Contain("\"status\":\"Finished\"");
//        }
//    }
//}
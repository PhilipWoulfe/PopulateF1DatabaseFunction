using F1.Api.Controllers;
using F1.Core.Interfaces;
using F1.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace F1.Api.Tests.Controllers
{
    public class DriversControllerTests
    {
        private readonly Mock<IDriverRepository> _mockRepo;
        private readonly DriversController _controller;

        public DriversControllerTests()
        {
            _mockRepo = new Mock<IDriverRepository>();
            _controller = new DriversController(_mockRepo.Object);
        }

        [Fact]
        public async Task GetDrivers_ReturnsOkResult_WithListOfDrivers()
        {
            // Arrange
            var drivers = new List<Driver>
            {
                new Driver { Id = "1", FullName = "Charles Leclerc" },
                new Driver { Id = "2", FullName = "Carlos Sainz" }
            };
            _mockRepo.Setup(repo => repo.GetDriversAsync()).ReturnsAsync(drivers);

            // Act
            var result = await _controller.GetDrivers();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<List<Driver>>(okResult.Value);
            Assert.Equal(2, returnValue.Count);
        }

        [Fact]
        public async Task GetDrivers_ReturnsNotFound_WhenNoDriversExist()
        {
            // Arrange
            _mockRepo.Setup(repo => repo.GetDriversAsync()).ReturnsAsync(new List<Driver>());

            // Act
            var result = await _controller.GetDrivers();

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}

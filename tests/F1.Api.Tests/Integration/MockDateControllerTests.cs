using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using F1.Api;
using F1.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace F1.Api.Tests.Integration
{
    public class MockDateControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public MockDateControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Can_Set_And_Get_MockDate()
        {
            var client = _factory.CreateClient();
            var setDate = new DateTime(2026, 3, 21, 15, 0, 0, DateTimeKind.Utc);
            var setResponse = await client.PostAsJsonAsync("/admin/mock-date", new { mockDateUtc = setDate });
            Assert.Equal(HttpStatusCode.NoContent, setResponse.StatusCode);

            var getResponse = await client.GetAsync("/admin/mock-date");
            getResponse.EnsureSuccessStatusCode();
            var result = await getResponse.Content.ReadFromJsonAsync<GetMockDateResponse>();
            Assert.NotNull(result);
            Assert.Equal(setDate, result.mockDate);
        }

        [Fact]
        public async Task Can_Clear_MockDate()
        {
            var client = _factory.CreateClient();
            var setResponse = await client.PostAsJsonAsync("/admin/mock-date", new { mockDateUtc = (DateTime?)null });
            Assert.Equal(HttpStatusCode.NoContent, setResponse.StatusCode);

            var getResponse = await client.GetAsync("/admin/mock-date");
            getResponse.EnsureSuccessStatusCode();
            var result = await getResponse.Content.ReadFromJsonAsync<GetMockDateResponse>();
            Assert.NotNull(result);
            Assert.Null(result.mockDate);
        }

        private class GetMockDateResponse
        {
            public DateTime? mockDate { get; set; }
        }
    }
}

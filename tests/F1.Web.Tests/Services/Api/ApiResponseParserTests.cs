using F1.Web.Models;
using F1.Web.Services.Api;
using System.Net;
using System.Text;
using System.Text.Json;

namespace F1.Web.Tests.Services.Api;

public class ApiResponseParserTests
{
    [Fact]
    public async Task EnsureSuccessAsync_WhenJsonErrorBodyExists_ThrowsApiServiceExceptionWithCode()
    {
        using var response = CreateJsonResponse(new { message = "Exactly 5 unique drivers must be selected.", code = "validation_error" }, HttpStatusCode.BadRequest);

        var ex = await Assert.ThrowsAsync<ApiServiceException>(() => ApiResponseParser.EnsureSuccessAsync(response, "Saving selection"));

        Assert.Equal(HttpStatusCode.BadRequest, ex.Error.StatusCode);
        Assert.Equal("Exactly 5 unique drivers must be selected.", ex.Error.Message);
        Assert.Equal("validation_error", ex.Error.Code);
    }

    [Fact]
    public async Task EnsureSuccessAsync_WhenPlainTextErrorBodyExists_ThrowsApiServiceExceptionWithBodyText()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Gateway timed out", Encoding.UTF8, "text/plain")
        };

        var ex = await Assert.ThrowsAsync<ApiServiceException>(() => ApiResponseParser.EnsureSuccessAsync(response, "Loading metadata"));

        Assert.Equal(HttpStatusCode.InternalServerError, ex.Error.StatusCode);
        Assert.Equal("Gateway timed out", ex.Error.Message);
    }

    [Fact]
    public async Task ReadRequiredJsonAsync_WhenJsonIsMalformed_ThrowsApiServiceException()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{ malformed", Encoding.UTF8, "application/json")
        };

        var ex = await Assert.ThrowsAsync<ApiServiceException>(() => ApiResponseParser.ReadRequiredJsonAsync<RaceConfig>(response, "Loading selection config"));

        Assert.Equal(HttpStatusCode.OK, ex.Error.StatusCode);
        Assert.Contains("malformed JSON", ex.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReadJsonOrDefaultAsync_WhenPayloadIsNull_ReturnsFallback()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        };

        var fallback = Array.Empty<Driver>();
        var result = await ApiResponseParser.ReadJsonOrDefaultAsync(response, fallback, "Loading drivers");

        Assert.Same(fallback, result);
    }

    private static HttpResponseMessage CreateJsonResponse<T>(T payload, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
    }
}

using F1.Web.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;

namespace F1.Web.Tests.Services;

public class DevMockAuthHandlerTests
{
    [Fact]
    public async Task SendAsync_ShouldInjectHeaders_InDevelopmentWhenEnabled()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DevSettings:SimulateCloudflare"] = "true",
                ["DevSettings:MockEmail"] = "dev-user@example.com"
            })
            .Build();

        var handler = new TestableDevMockAuthHandler(
            new TestHostEnvironment("Development"),
            configuration,
            new TerminalHandler());

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/results");
        await handler.InvokeSendAsync(request, CancellationToken.None);

        Assert.True(request.Headers.Contains("Cf-Access-Authenticated-User-Email"));
        Assert.True(request.Headers.Contains("Cf-Access-Jwt-Assertion"));
    }

    [Fact]
    public async Task SendAsync_ShouldNotInjectHeaders_WhenNotDevelopment()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DevSettings:SimulateCloudflare"] = "true",
                ["DevSettings:MockEmail"] = "dev-user@example.com"
            })
            .Build();

        var handler = new TestableDevMockAuthHandler(
            new TestHostEnvironment("Production"),
            configuration,
            new TerminalHandler());

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/results");
        await handler.InvokeSendAsync(request, CancellationToken.None);

        Assert.False(request.Headers.Contains("Cf-Access-Authenticated-User-Email"));
        Assert.False(request.Headers.Contains("Cf-Access-Jwt-Assertion"));
    }

    private sealed class TestableDevMockAuthHandler : DevMockAuthHandler
    {
        public TestableDevMockAuthHandler(IWebAssemblyHostEnvironment env, IConfiguration config, HttpMessageHandler innerHandler)
            : base(env, config)
        {
            InnerHandler = innerHandler;
        }

        public Task<HttpResponseMessage> InvokeSendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return SendAsync(request, cancellationToken);
        }
    }

    private sealed class TestHostEnvironment : IWebAssemblyHostEnvironment
    {
        public TestHostEnvironment(string environment)
        {
            Environment = environment;
        }

        public string Environment { get; }
        public string ApplicationName => "F1.Web.Tests";
        public string BaseAddress => "http://localhost/";
    }

    private sealed class TerminalHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}

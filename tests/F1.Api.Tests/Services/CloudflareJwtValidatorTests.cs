using F1.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace F1.Api.Tests.Services;

public class CloudflareJwtValidatorTests
{
    [Fact]
    public async Task ValidateAsync_ReturnsSuccess_ForValidToken()
    {
        const string issuer = "https://f1-team.cloudflareaccess.com";
        const string audience = "test-audience";

        var (token, key) = CreateToken(issuer, audience, DateTime.UtcNow.AddMinutes(15));
        var jwksJson = BuildJwksJson(key);
        var handler = new StaticResponseHandler(jwksJson);
        var validator = CreateValidator(handler, issuer, audience);

        var result = await validator.ValidateAsync(token, CancellationToken.None);

        Assert.True(result.IsValid);
        Assert.NotNull(result.Principal);
        Assert.Equal("driver@f1.com", result.Principal!.FindFirst("email")?.Value);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsFailure_ForExpiredToken()
    {
        const string issuer = "https://f1-team.cloudflareaccess.com";
        const string audience = "test-audience";

        var (token, key) = CreateToken(issuer, audience, DateTime.UtcNow.AddMinutes(-10));
        var jwksJson = BuildJwksJson(key);
        var handler = new StaticResponseHandler(jwksJson);
        var validator = CreateValidator(handler, issuer, audience);

        var result = await validator.ValidateAsync(token, CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Contains("expired", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsFailure_ForInvalidAudience()
    {
        const string issuer = "https://f1-team.cloudflareaccess.com";

        var (token, key) = CreateToken(issuer, "unexpected-audience", DateTime.UtcNow.AddMinutes(15));
        var jwksJson = BuildJwksJson(key);
        var handler = new StaticResponseHandler(jwksJson);
        var validator = CreateValidator(handler, issuer, "expected-audience");

        var result = await validator.ValidateAsync(token, CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Contains("invalid", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateAsync_UsesCachedJwks_BetweenCalls()
    {
        const string issuer = "https://f1-team.cloudflareaccess.com";
        const string audience = "test-audience";

        var (token, key) = CreateToken(issuer, audience, DateTime.UtcNow.AddMinutes(15));
        var jwksJson = BuildJwksJson(key);
        var handler = new StaticResponseHandler(jwksJson);
        var validator = CreateValidator(handler, issuer, audience);

        var first = await validator.ValidateAsync(token, CancellationToken.None);
        var second = await validator.ValidateAsync(token, CancellationToken.None);

        Assert.True(first.IsValid);
        Assert.True(second.IsValid);
        Assert.Equal(1, handler.CallCount);
    }

    private static CloudflareJwtValidator CreateValidator(HttpMessageHandler handler, string issuer, string audience)
    {
        var httpClient = new HttpClient(handler);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new CloudflareAccessOptions
        {
            CertsUrl = "https://f1-team.cloudflareaccess.com/cdn-cgi/access/certs",
            Issuer = issuer,
            Audience = audience,
            JwksCacheHours = 24
        });

        return new CloudflareJwtValidator(httpClient, cache, options, NullLogger<CloudflareJwtValidator>.Instance);
    }

    private static (string Token, RsaSecurityKey Key) CreateToken(string issuer, string audience, DateTime expiresUtc)
    {
        var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = Guid.NewGuid().ToString("N") };
        var credentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", "subject-123"),
                new Claim("email", "driver@f1.com"),
                new Claim("name", "Driver One")
            }),
            NotBefore = expiresUtc.AddMinutes(-30),
            Expires = expiresUtc,
            SigningCredentials = credentials
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(tokenDescriptor) as JwtSecurityToken;
        token!.Header["kid"] = key.KeyId;

        return (handler.WriteToken(token), key);
    }

    private static string BuildJwksJson(RsaSecurityKey key)
    {
        var parameters = key.Rsa?.ExportParameters(false) ?? key.Parameters;
        var modulus = Base64UrlEncoder.Encode(parameters.Modulus);
        var exponent = Base64UrlEncoder.Encode(parameters.Exponent);

        return "{" +
               "\"keys\":[{" +
               "\"kty\":\"RSA\"," +
               $"\"kid\":\"{key.KeyId}\"," +
               "\"use\":\"sig\"," +
               "\"alg\":\"RS256\"," +
               $"\"n\":\"{modulus}\"," +
               $"\"e\":\"{exponent}\"" +
               "}]}";
    }

    private sealed class StaticResponseHandler : HttpMessageHandler
    {
        private readonly string _jwksJson;

        public int CallCount { get; private set; }

        public StaticResponseHandler(string jwksJson)
        {
            _jwksJson = jwksJson;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_jwksJson, Encoding.UTF8, "application/json")
            });
        }
    }
}

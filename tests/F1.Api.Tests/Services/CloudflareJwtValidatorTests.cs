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
        Assert.Equal("token_expired", result.ReasonCode);
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
        Assert.Equal("token_invalid", result.ReasonCode);
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

    [Fact]
    public async Task ValidateAsync_RefreshesJwks_WhenKidNotFoundInCache()
    {
        const string issuer = "https://f1-team.cloudflareaccess.com";
        const string audience = "test-audience";

        // First key and token: populate cache with a JWKS that only contains key1
        var (token1, key1) = CreateToken(issuer, audience, DateTime.UtcNow.AddMinutes(15));
        var jwksJson1 = BuildJwksJson(key1);
        var handler1 = new StaticResponseHandler(jwksJson1);

        var sharedCache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new CloudflareAccessOptions
        {
            CertsUrl = "https://f1-team.cloudflareaccess.com/cdn-cgi/access/certs",
            Issuer = issuer,
            Audience = audience,
            JwksCacheHours = 24
        });

        var validator1 = new CloudflareJwtValidator(
            new HttpClient(handler1),
            sharedCache,
            options,
            NullLogger<CloudflareJwtValidator>.Instance);

        var firstResult = await validator1.ValidateAsync(token1, CancellationToken.None);
        Assert.True(firstResult.IsValid);

        // Second key and token: the cached JWKS does not contain key2's kid, so a refresh is required
        var (token2, key2) = CreateToken(issuer, audience, DateTime.UtcNow.AddMinutes(15));
        var jwksJson2 = BuildJwksJson(key2);
        var handler2 = new StaticResponseHandler(jwksJson2);

        var validator2 = new CloudflareJwtValidator(
            new HttpClient(handler2),
            sharedCache,
            options,
            NullLogger<CloudflareJwtValidator>.Instance);

        var secondResult = await validator2.ValidateAsync(token2, CancellationToken.None);

        Assert.True(secondResult.IsValid);
        // One JWKS fetch to populate the cache, and one more after kid mismatch forces a refresh
        Assert.Equal(1, handler1.CallCount);
        Assert.Equal(1, handler2.CallCount);
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

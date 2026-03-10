using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace F1.Api.Services;

public class CloudflareJwtValidator : ICloudflareJwtValidator
{
    private const string JwksCacheKey = "CloudflareAccess:Jwks";
    private readonly SemaphoreSlim _jwksRefreshLock = new(1, 1);

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _memoryCache;
    private readonly IOptions<CloudflareAccessOptions> _options;
    private readonly ILogger<CloudflareJwtValidator> _logger;

    public CloudflareJwtValidator(
        HttpClient httpClient,
        IMemoryCache memoryCache,
        IOptions<CloudflareAccessOptions> options,
        ILogger<CloudflareJwtValidator> logger)
    {
        _httpClient = httpClient;
        _memoryCache = memoryCache;
        _options = options;
        _logger = logger;
    }

    public async Task<CloudflareTokenValidationResult> ValidateAsync(string jwtAssertion, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(jwtAssertion))
        {
            return CloudflareTokenValidationResult.Failure("Unauthorized: missing Cloudflare Access token.");
        }

        var jwtHandler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        JwtSecurityToken unvalidatedToken;

        try
        {
            unvalidatedToken = jwtHandler.ReadJwtToken(jwtAssertion);
        }
        catch (Exception)
        {
            return CloudflareTokenValidationResult.Failure("Unauthorized: malformed Cloudflare Access token.");
        }

        var kid = unvalidatedToken.Header.Kid;
        if (string.IsNullOrWhiteSpace(kid))
        {
            return CloudflareTokenValidationResult.Failure("Unauthorized: invalid Cloudflare Access token.");
        }

        var options = _options.Value;
        if (string.IsNullOrWhiteSpace(options.Audience) || string.IsNullOrWhiteSpace(options.Issuer))
        {
            _logger.LogWarning("CloudflareAccess options are incomplete. Audience/Issuer must be configured.");
            return CloudflareTokenValidationResult.Failure("Unauthorized: Cloudflare Access configuration is invalid.");
        }

        JsonWebKeySet jwks;
        List<SecurityKey> signingKeys;

        try
        {
            jwks = await GetJwksAsync(forceRefresh: false, cancellationToken);
            signingKeys = jwks.Keys.Where(key => string.Equals(key.Kid, kid, StringComparison.Ordinal)).Cast<SecurityKey>().ToList();

            if (signingKeys.Count == 0)
            {
                jwks = await GetJwksAsync(forceRefresh: true, cancellationToken);
                signingKeys = jwks.Keys.Where(key => string.Equals(key.Kid, kid, StringComparison.Ordinal)).Cast<SecurityKey>().ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to fetch or parse Cloudflare JWKS.");
            return CloudflareTokenValidationResult.Failure("Unauthorized: unable to verify Cloudflare Access token.");
        }

        if (signingKeys.Count == 0)
        {
            return CloudflareTokenValidationResult.Failure("Unauthorized: invalid Cloudflare Access token.");
        }

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = signingKeys,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        try
        {
            var principal = jwtHandler.ValidateToken(jwtAssertion, validationParameters, out _);
            return CloudflareTokenValidationResult.Success(principal);
        }
        catch (SecurityTokenExpiredException)
        {
            return CloudflareTokenValidationResult.Failure("Unauthorized: Cloudflare Access token has expired.");
        }
        catch (SecurityTokenException)
        {
            return CloudflareTokenValidationResult.Failure("Unauthorized: invalid Cloudflare Access token.");
        }
        catch (Exception)
        {
            return CloudflareTokenValidationResult.Failure("Unauthorized: unable to verify Cloudflare Access token.");
        }
    }

    private async Task<JsonWebKeySet> GetJwksAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        if (!forceRefresh && _memoryCache.TryGetValue<JsonWebKeySet>(JwksCacheKey, out var cachedJwks) && cachedJwks is not null)
        {
            return cachedJwks;
        }

        await _jwksRefreshLock.WaitAsync(cancellationToken);
        try
        {
            if (!forceRefresh && _memoryCache.TryGetValue<JsonWebKeySet>(JwksCacheKey, out cachedJwks) && cachedJwks is not null)
            {
                return cachedJwks;
            }

            var certsUrl = _options.Value.CertsUrl;
            using var response = await _httpClient.GetAsync(certsUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jwksJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var jwks = new JsonWebKeySet(jwksJson);

            var cacheTtlHours = _options.Value.JwksCacheHours;
            var expiresAt = DateTimeOffset.UtcNow.AddHours(cacheTtlHours <= 0 ? 24 : cacheTtlHours);
            _memoryCache.Set(JwksCacheKey, jwks, expiresAt);

            return jwks;
        }
        finally
        {
            _jwksRefreshLock.Release();
        }
    }
}

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace F1.Api.Services;

public class CloudflareJwtValidator : ICloudflareJwtValidator
{
    private const string JwksCacheKey = "CloudflareAccess:Jwks";
    private static readonly SemaphoreSlim JwksRefreshLock = new(1, 1);
    private static readonly TimeSpan ForceRefreshCooldown = TimeSpan.FromSeconds(30);
    private static DateTimeOffset _lastForcedRefreshAtUtc = DateTimeOffset.MinValue;

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
            return CloudflareTokenValidationResult.Failure("malformed_jwt");
        }

        var jwtHandler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        JwtSecurityToken unvalidatedToken;

        try
        {
            unvalidatedToken = jwtHandler.ReadJwtToken(jwtAssertion);
        }
        catch (Exception ex)
        {
            return CloudflareTokenValidationResult.Failure("malformed_jwt", ex: ex);
        }

        var kid = unvalidatedToken.Header.Kid;
        if (string.IsNullOrWhiteSpace(kid))
        {
            return CloudflareTokenValidationResult.Failure("missing_kid");
        }

        var options = _options.Value;
        if (string.IsNullOrWhiteSpace(options.Audience) || string.IsNullOrWhiteSpace(options.Issuer))
        {
            return CloudflareTokenValidationResult.Failure("config_invalid", kidPresent: true);
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
            return CloudflareTokenValidationResult.Failure("jwks_fetch_failed", kidPresent: true, ex: ex);
        }

        if (signingKeys.Count == 0)
        {
            return CloudflareTokenValidationResult.Failure("signing_key_not_found", kidPresent: true);
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
        catch (SecurityTokenExpiredException ex)
        {
            return CloudflareTokenValidationResult.Failure("token_expired", kidPresent: true, ex: ex);
        }
        catch (SecurityTokenException ex)
        {
            return CloudflareTokenValidationResult.Failure("token_invalid", kidPresent: true, ex: ex);
        }
        catch (Exception ex)
        {
            return CloudflareTokenValidationResult.Failure("token_validation_unexpected_error", kidPresent: true, ex: ex);
        }
    }

    private async Task<JsonWebKeySet> GetJwksAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        if (!forceRefresh && _memoryCache.TryGetValue<JsonWebKeySet>(JwksCacheKey, out var cachedJwks) && cachedJwks is not null)
        {
            return cachedJwks;
        }

        await JwksRefreshLock.WaitAsync(cancellationToken);
        try
        {
            if (_memoryCache.TryGetValue<JsonWebKeySet>(JwksCacheKey, out cachedJwks) && cachedJwks is not null)
            {
                if (!forceRefresh)
                {
                    return cachedJwks;
                }

                // Avoid stampeding Cloudflare with repeated forced refreshes when many requests fail key lookup.
                if (DateTimeOffset.UtcNow - _lastForcedRefreshAtUtc < ForceRefreshCooldown)
                {
                    return cachedJwks;
                }
            }

            var certsUrl = _options.Value.CertsUrl;
            using var response = await _httpClient.GetAsync(certsUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jwksJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var jwks = new JsonWebKeySet(jwksJson);

            var cacheTtlHours = _options.Value.JwksCacheHours;
            var expiresAt = DateTimeOffset.UtcNow.AddHours(cacheTtlHours <= 0 ? 24 : cacheTtlHours);
            _memoryCache.Set(JwksCacheKey, jwks, expiresAt);

            _logger.LogDebug(
                 "Fetched Cloudflare JWKS from {CertsUrl}. ForceRefresh={ForceRefresh}, ExpiresAtUtc={ExpiresAtUtc}",
                 certsUrl,
                 forceRefresh,
                 expiresAt);

            if (forceRefresh)
            {
                _lastForcedRefreshAtUtc = DateTimeOffset.UtcNow;
            }

            return jwks;
        }
        finally
        {
            JwksRefreshLock.Release();
        }
    }
}

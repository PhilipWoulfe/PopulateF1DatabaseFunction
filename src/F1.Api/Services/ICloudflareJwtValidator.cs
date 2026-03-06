using System.Security.Claims;

namespace F1.Api.Services;

public interface ICloudflareJwtValidator
{
    Task<CloudflareTokenValidationResult> ValidateAsync(string jwtAssertion, CancellationToken cancellationToken);
}

public sealed record CloudflareTokenValidationResult(bool IsValid, ClaimsPrincipal? Principal, string ErrorMessage)
{
    public static CloudflareTokenValidationResult Success(ClaimsPrincipal principal) => new(true, principal, string.Empty);

    public static CloudflareTokenValidationResult Failure(string errorMessage) => new(false, null, errorMessage);
}

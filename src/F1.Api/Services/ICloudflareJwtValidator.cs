namespace F1.Api.Services;

public interface ICloudflareJwtValidator
{
    Task<CloudflareTokenValidationResult> ValidateAsync(string jwtAssertion, CancellationToken cancellationToken);
}

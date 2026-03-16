using System.Security.Claims;

namespace F1.Api.Services;

public sealed record CloudflareTokenValidationResult(
    bool IsValid,
    ClaimsPrincipal? Principal,
    string ErrorMessage,
    string ReasonCode = "",
    bool KidPresent = false,
    Exception? Exception = null)
{
    public static CloudflareTokenValidationResult Success(ClaimsPrincipal principal) =>
        new(true, principal, string.Empty);

    public static CloudflareTokenValidationResult Failure(
        string reasonCode,
        bool kidPresent = false,
        Exception? ex = null) =>
        new(false, null, "Unauthorized.", reasonCode, kidPresent, ex);
}

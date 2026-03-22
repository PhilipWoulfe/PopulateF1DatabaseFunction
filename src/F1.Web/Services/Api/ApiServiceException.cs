namespace F1.Web.Services.Api;

public sealed class ApiServiceException : Exception
{
    public ApiServiceException(ApiError error, Exception? innerException = null)
        : base(error.Message, innerException)
    {
        Error = error;
    }

    public ApiError Error { get; }
}

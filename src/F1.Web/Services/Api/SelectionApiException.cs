using System.Net;

namespace F1.Web.Services.Api;

public sealed class SelectionApiException : Exception
{
    public SelectionApiException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}

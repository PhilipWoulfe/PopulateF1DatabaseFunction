using System.Net;

namespace F1.Web.Services.Api;

public sealed record ApiError(HttpStatusCode StatusCode, string Message, string? Code = null);

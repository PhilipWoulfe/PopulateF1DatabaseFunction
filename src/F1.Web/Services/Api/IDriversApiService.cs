using F1.Web.Models;

namespace F1.Web.Services.Api;

public interface IDriversApiService
{
    Task<Driver[]> GetAllAsync(CancellationToken cancellationToken = default);
}

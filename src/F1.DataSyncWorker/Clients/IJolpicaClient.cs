using F1.DataSyncWorker.Models;

namespace F1.DataSyncWorker.Clients;

public interface IJolpicaClient
{
    Task<IReadOnlyList<JolpicaDriverDto>> GetDriversAsync(int season, int retryCount, int retryDelayMs, CancellationToken cancellationToken);
    Task<IReadOnlyList<JolpicaRaceDto>> GetRacesAsync(int season, int retryCount, int retryDelayMs, CancellationToken cancellationToken);
}
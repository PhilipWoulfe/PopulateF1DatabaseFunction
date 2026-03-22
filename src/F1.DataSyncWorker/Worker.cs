using F1.DataSyncWorker.Options;
using F1.DataSyncWorker.Services;
using Microsoft.Extensions.Options;

namespace F1.DataSyncWorker;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IDataSyncOrchestrator _orchestrator;
    private readonly DataSyncOptions _options;

    public Worker(
        ILogger<Worker> logger,
        IDataSyncOrchestrator orchestrator,
        IOptions<DataSyncOptions> options)
    {
        _logger = logger;
        _orchestrator = orchestrator;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("F1 data sync worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _orchestrator.RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Data sync run failed.");

                if (!_options.ContinueOnError)
                {
                    throw;
                }
            }

            if (_options.IntervalMinutes <= 0)
            {
                _logger.LogInformation("IntervalMinutes is {IntervalMinutes}. Exiting after single run.", _options.IntervalMinutes);
                break;
            }

            var delay = TimeSpan.FromMinutes(_options.IntervalMinutes);
            _logger.LogInformation("Next data sync run scheduled in {DelayMinutes} minutes.", _options.IntervalMinutes);
            await Task.Delay(delay, stoppingToken);
        }

        _logger.LogInformation("F1 data sync worker stopped.");
    }
}

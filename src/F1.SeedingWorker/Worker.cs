using F1.SeedingWorker.Options;
using F1.SeedingWorker.Services;
using Microsoft.Extensions.Options;

namespace F1.SeedingWorker;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ISeedOrchestrator _orchestrator;
    private readonly SeedOptions _options;

    public Worker(
        ILogger<Worker> logger,
        ISeedOrchestrator orchestrator,
        IOptions<SeedOptions> options)
    {
        _logger = logger;
        _orchestrator = orchestrator;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("F1 seeding worker started.");

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
                _logger.LogError(ex, "Seeding run failed.");

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
            _logger.LogInformation("Next seeding run scheduled in {DelayMinutes} minutes.", _options.IntervalMinutes);
            await Task.Delay(delay, stoppingToken);
        }

        _logger.LogInformation("F1 seeding worker stopped.");
    }
}

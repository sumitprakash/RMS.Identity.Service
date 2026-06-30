using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RMS.Identity.Service.Infrastructure.Maintenance;

public sealed class DataRetentionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DataRetentionOptions _options;
    private readonly ILogger<DataRetentionWorker> _logger;

    public DataRetentionWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<DataRetentionOptions> options,
        ILogger<DataRetentionWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            using var disabledScope = BeginCorrelationScope("data-retention-disabled");
            _logger.LogInformation("Data retention worker is disabled.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromHours(_options.RunIntervalHours));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = BeginCorrelationScope($"data-retention-{Guid.NewGuid():N}");
            try
            {
                using var serviceScope = _scopeFactory.CreateScope();
                var repository = serviceScope.ServiceProvider.GetRequiredService<DataRetentionRepository>();
                var deleted = await repository.PurgeAsync(_options, stoppingToken);
                _logger.LogInformation("Data retention purge deleted {DeletedRecordCount} records.", deleted);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Data retention purge failed.");
            }
        }
    }

    private IDisposable? BeginCorrelationScope(string correlationTraceId) =>
        _logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationTraceId"] = correlationTraceId
        });
}

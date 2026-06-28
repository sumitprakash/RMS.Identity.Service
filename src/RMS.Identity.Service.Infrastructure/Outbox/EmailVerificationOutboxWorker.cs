using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RMS.Identity.Service.Infrastructure.Notifications;

namespace RMS.Identity.Service.Infrastructure.Outbox;

public sealed class EmailVerificationOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EmailDeliveryOptions _options;
    private readonly ILogger<EmailVerificationOutboxWorker> _logger;

    public EmailVerificationOutboxWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<EmailDeliveryOptions> options,
        ILogger<EmailVerificationOutboxWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled && !_options.AutoVerifyByEndpoint)
        {
            using var disabledScope = BeginCorrelationScope("email-verification-outbox-disabled");
            _logger.LogInformation("Email verification outbox worker is disabled.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.PollIntervalSeconds));
        do
        {
            await ProcessBatchAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = BeginCorrelationScope($"email-verification-outbox-{Guid.NewGuid():N}");
        try
        {
            using var serviceScope = _scopeFactory.CreateScope();
            var processor = serviceScope.ServiceProvider.GetRequiredService<EmailVerificationRequestedOutboxProcessor>();
            await processor.ProcessBatchAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Email verification outbox worker failed while processing a batch.");
        }
    }

    private IDisposable? BeginCorrelationScope(string correlationTraceId) =>
        _logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationTraceId"] = correlationTraceId
        });
}

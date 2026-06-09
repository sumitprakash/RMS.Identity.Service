using RMS.Identity.Service.Domain.Entities.Outbox;

namespace RMS.Identity.Service.Domain.Interfaces.Repositories.Outbox;

public interface IOutboxProcessingRepository
{
    Task<IReadOnlyCollection<OutboxMessage>> ClaimAvailableAsync(
        string eventType,
        int batchSize,
        int maxRetries,
        int processingTimeoutSeconds,
        CancellationToken cancellationToken);

    Task MarkPublishedAsync(
        long outboxId,
        CancellationToken cancellationToken);

    Task MarkFailedAsync(
        long outboxId,
        DateTime availableAt,
        CancellationToken cancellationToken);
}

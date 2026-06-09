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

    Task<bool> MarkPublishedAsync(
        long outboxId,
        DateTime processingLeaseExpiresAt,
        CancellationToken cancellationToken);

    Task<bool> MarkFailedAsync(
        long outboxId,
        DateTime processingLeaseExpiresAt,
        DateTime availableAt,
        CancellationToken cancellationToken);
}

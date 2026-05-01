using RMS.Identity.Service.Domain.Contracts.Outbox;

namespace RMS.Identity.Service.Domain.Interfaces.Outbox;

public interface IOutboxRepository
{
    Task EnqueueAsync(
        VerificationEmailOutboxMessage message,
        CancellationToken cancellationToken);
}

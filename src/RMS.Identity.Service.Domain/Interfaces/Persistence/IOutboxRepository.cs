using RMS.Identity.Service.Domain.Entities;

namespace RMS.Identity.Service.Domain.Interfaces.Persistence;

public interface IOutboxRepository
{
    Task CreateAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}

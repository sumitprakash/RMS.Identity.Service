using RMS.Identity.Service.Domain.Contracts.Idempotency;

namespace RMS.Identity.Service.Domain.Interfaces.Repositories.Idempotency;

public interface IIdempotencyReadRepository
{
    Task<IdempotencyRecord?> GetAsync(
        string key,
        bool lockForUpdate,
        CancellationToken cancellationToken);
}

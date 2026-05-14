using RMS.Identity.Service.Domain.Contracts.Idempotency;

namespace RMS.Identity.Service.Domain.Interfaces.Repositories.Idempotency;

public interface IIdempotencyRepository
{
    Task<IdempotencyRecord?> GetAsync(
        string key,
        bool lockForUpdate,
        CancellationToken cancellationToken);

    Task<bool> TryCreateAsync(
        IdempotencyRequest request,
        CancellationToken cancellationToken);

    Task StoreResponseAsync(
        string key,
        int responseCode,
        string responseBody,
        CancellationToken cancellationToken);
}

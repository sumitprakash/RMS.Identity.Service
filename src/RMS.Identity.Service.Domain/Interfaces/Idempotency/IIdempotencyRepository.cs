using RMS.Identity.Service.Domain.Contracts.Idempotency;
using RMS.Identity.Service.Domain.Interfaces.Persistence;

namespace RMS.Identity.Service.Domain.Interfaces.Idempotency;

public interface IIdempotencyRepository
{
    Task<IdempotencyRecord?> GetAsync(
        IDatabaseTransaction transaction,
        string key,
        bool lockForUpdate,
        CancellationToken cancellationToken);

    Task<bool> TryCreateAsync(
        IDatabaseTransaction transaction,
        IdempotencyRequest request,
        CancellationToken cancellationToken);

    Task StoreResponseAsync(
        IDatabaseTransaction transaction,
        string key,
        int responseCode,
        string responseBody,
        CancellationToken cancellationToken);
}

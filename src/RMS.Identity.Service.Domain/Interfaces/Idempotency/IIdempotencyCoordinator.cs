using RMS.Identity.Service.Domain.Contracts.Idempotency;
using RMS.Identity.Service.Domain.Interfaces.Persistence;

namespace RMS.Identity.Service.Domain.Interfaces.Idempotency;

public interface IIdempotencyCoordinator
{
    Task<IdempotencyReservationResult<TResponse>> ReserveAsync<TResponse>(
        IDatabaseTransaction transaction,
        IdempotencyRequest request,
        CancellationToken cancellationToken);

    Task StoreResponseAsync<TResponse>(
        IDatabaseTransaction transaction,
        string key,
        int responseCode,
        TResponse response,
        CancellationToken cancellationToken);
}

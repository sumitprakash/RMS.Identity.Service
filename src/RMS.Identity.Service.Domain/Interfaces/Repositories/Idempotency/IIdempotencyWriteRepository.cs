using RMS.Identity.Service.Domain.Contracts.Idempotency;

namespace RMS.Identity.Service.Domain.Interfaces.Repositories.Idempotency;

public interface IIdempotencyWriteRepository
{
    Task<bool> TryCreateAsync(
        IdempotencyRequest request,
        CancellationToken cancellationToken);

    Task StoreResponseAsync(
        string key,
        int responseCode,
        string responseBody,
        CancellationToken cancellationToken);
}

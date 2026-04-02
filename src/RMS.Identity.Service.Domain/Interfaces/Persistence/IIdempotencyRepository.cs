using RMS.Identity.Service.Domain.Entities;

namespace RMS.Identity.Service.Domain.Interfaces.Persistence;

public interface IIdempotencyRepository
{
    Task<IdempotencyKeyRecord?> GetAsync(string keyValue, CancellationToken cancellationToken = default);

    Task CreateAsync(IdempotencyKeyRecord record, CancellationToken cancellationToken = default);
}

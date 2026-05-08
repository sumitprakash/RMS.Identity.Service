using RMS.Identity.Service.Domain.Contracts.Idempotency;

namespace RMS.Identity.Service.Api.Shared.Idempotency;

public interface IIdempotencyRequestFactory
{
    Task<IdempotencyRequest> CreateAsync(HttpRequest request, CancellationToken cancellationToken);
}

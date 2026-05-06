using RMS.Identity.Service.Domain.Contracts.Idempotency;

namespace RMS.Identity.Service.Application.Shared.Idempotency;

public interface IIdempotencyRequestFactory
{
    IdempotencyRequest Create<TRequest>(
        Guid idempotencyKey,
        string method,
        string route,
        TRequest request)
        where TRequest : notnull;
}

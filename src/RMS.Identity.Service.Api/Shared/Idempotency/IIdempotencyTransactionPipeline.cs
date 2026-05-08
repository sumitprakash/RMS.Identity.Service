using RMS.Identity.Service.Domain.Contracts.Idempotency;

namespace RMS.Identity.Service.Api.Shared.Idempotency;

public interface IIdempotencyTransactionPipeline
{
    Task<IdempotencyMiddlewareResponse> ExecuteAsync(
        HttpContext context,
        IdempotencyRequest idempotencyRequest,
        RequestDelegate next,
        CancellationToken cancellationToken);
}

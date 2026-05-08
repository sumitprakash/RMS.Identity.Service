namespace RMS.Identity.Service.Api.Shared.Idempotency;

public interface IIdempotencyResponseCapture
{
    Task<IdempotencyMiddlewareResponse> CaptureAsync(
        HttpContext context,
        RequestDelegate next,
        CancellationToken cancellationToken);
}

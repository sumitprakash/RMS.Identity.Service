namespace RMS.Identity.Service.Api.Shared.Idempotency;

public interface IIdempotencyService
{
    Task ExecuteAsync(
        HttpContext context,
        RequestDelegate next,
        CancellationToken cancellationToken);
}

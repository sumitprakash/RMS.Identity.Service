using RMS.Identity.Service.Domain.Contracts.Idempotency;

namespace RMS.Identity.Service.Api.Shared.Idempotency;

public sealed record IdempotencyMiddlewareResponse(
    int StatusCode,
    string? ContentType,
    string Body)
{
    public static IdempotencyMiddlewareResponse FromStored(IdempotencyStoredResponse storedResponse) =>
        new(storedResponse.StatusCode, "application/json", storedResponse.ResponseBody);

    public async Task WriteToAsync(HttpResponse response, CancellationToken cancellationToken)
    {
        response.StatusCode = StatusCode;

        if (!string.IsNullOrWhiteSpace(ContentType))
        {
            response.ContentType = ContentType;
        }

        await response.WriteAsync(Body, cancellationToken);
    }
}

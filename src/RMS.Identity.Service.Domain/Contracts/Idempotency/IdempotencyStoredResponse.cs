namespace RMS.Identity.Service.Domain.Contracts.Idempotency;

public sealed record IdempotencyStoredResponse(
    int StatusCode,
    string ResponseBody);

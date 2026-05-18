namespace RMS.Identity.Service.Domain.Contracts.Idempotency;

public sealed record IdempotencyRecord(
    string Method,
    string Route,
    string? RequestHash,
    int? ResponseCode,
    string? ResponseBody);

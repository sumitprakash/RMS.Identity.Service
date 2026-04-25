namespace RMS.Identity.Service.Domain.Contracts.Idempotency;

public sealed record IdempotencyRequest(
    string Key,
    string Method,
    string Route,
    string? RequestHash);

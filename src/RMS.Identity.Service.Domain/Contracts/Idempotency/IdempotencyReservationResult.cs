namespace RMS.Identity.Service.Domain.Contracts.Idempotency;

public sealed record IdempotencyReservationResult<TResponse>(
    bool IsReserved,
    TResponse? StoredResponse);

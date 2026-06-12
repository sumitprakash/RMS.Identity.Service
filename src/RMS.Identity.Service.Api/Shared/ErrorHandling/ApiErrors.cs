using RMS.Identity.Service.Domain.Shared.Errors;

namespace RMS.Identity.Service.Api.Shared.ErrorHandling;

public static class ApiErrors
{
    public static class BadRequest
    {
        public static readonly ServiceError ValidationError = new(
            new ServiceErrorCode(1, 3),
            "Request validation failed.");

        public static readonly ServiceError IdempotencyKeyRequired = new(
            new ServiceErrorCode(7, 1),
            "Idempotency-Key is required.");

        public static readonly ServiceError IdempotencyKeyInvalidUuid = new(
            new ServiceErrorCode(7, 2),
            "Idempotency-Key must be a valid UUID.");
    }
}

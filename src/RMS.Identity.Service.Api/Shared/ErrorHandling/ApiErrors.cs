using RMS.Identity.Service.Application.Shared.Errors;

namespace RMS.Identity.Service.Api.Shared.ErrorHandling;

public static class ApiErrors
{
    public static class BadRequest
    {
        public static readonly ServiceError ValidationError = new(
            ServiceStatusErrorCodes.BadRequest,
            new ServiceErrorCode(1, 3),
            "Request validation failed.");

        public static readonly ServiceError IdempotencyKeyRequired = new(
            ServiceStatusErrorCodes.BadRequest,
            new ServiceErrorCode(7, 1),
            "Idempotency-Key is required.");

        public static readonly ServiceError IdempotencyKeyInvalidUuid = new(
            ServiceStatusErrorCodes.BadRequest,
            new ServiceErrorCode(7, 2),
            "Idempotency-Key must be a valid UUID.");
    }

    public static class Conflict
    {
        public static readonly ServiceError IdempotencyKeyReusedForDifferentRequest = new(
            ServiceStatusErrorCodes.Conflict,
            new ServiceErrorCode(7, 3),
            "Idempotency key was already used for a different request.");

        public static readonly ServiceError IdempotencyKeyPayloadMismatch = new(
            ServiceStatusErrorCodes.Conflict,
            new ServiceErrorCode(7, 4),
            "Idempotency key payload does not match the original request.");

        public static readonly ServiceError IdempotencyRequestInProgress = new(
            ServiceStatusErrorCodes.Conflict,
            new ServiceErrorCode(7, 5),
            "Idempotent request is already in progress.");
    }
}

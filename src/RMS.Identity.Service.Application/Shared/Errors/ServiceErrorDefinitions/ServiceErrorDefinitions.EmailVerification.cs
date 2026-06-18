namespace RMS.Identity.Service.Application.Shared.Errors;

public static partial class ServiceErrorDefinitions
{
    public static class EmailVerification
    {
        public static readonly ServiceError EmailVerificationNotFound = new(
            ServiceStatusErrorCodes.NotFound,
            new ServiceErrorCode(6, 1),
            "Email verification token could not be found.");

        public static readonly ServiceError EmailVerificationAlreadyUsed = new(
            ServiceStatusErrorCodes.BadRequest,
            new ServiceErrorCode(6, 2),
            "Email verification token has already been used.");

        public static readonly ServiceError EmailVerificationExpired = new(
            ServiceStatusErrorCodes.BadRequest,
            new ServiceErrorCode(6, 3),
            "Email verification token has expired.");
    }
}

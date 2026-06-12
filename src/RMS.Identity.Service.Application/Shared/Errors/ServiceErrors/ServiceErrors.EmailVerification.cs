namespace RMS.Identity.Service.Application.Shared.Errors;

public static partial class ServiceErrors
{
    public static class EmailVerification
    {
        public static readonly ServiceError EmailVerificationNotFound = new(
            new ServiceErrorCode(6, 1),
            "Email verification token could not be found.");

        public static readonly ServiceError EmailVerificationAlreadyUsed = new(
            new ServiceErrorCode(6, 2),
            "Email verification token has already been used.");

        public static readonly ServiceError EmailVerificationExpired = new(
            new ServiceErrorCode(6, 3),
            "Email verification token has expired.");
    }
}

namespace RMS.Identity.Service.Application.Shared.Errors;

public static partial class ServiceErrors
{
    public static class Auth
    {
        public static readonly ServiceError InvalidCredentials = new(
            new ServiceErrorCode(2, 1),
            "Username or password is incorrect.");

        public static readonly ServiceError InvalidRefreshToken = new(
            new ServiceErrorCode(2, 2),
            "Refresh token is invalid.");

        public static readonly ServiceError AccountInactive = new(
            new ServiceErrorCode(2, 3),
            "User account is inactive.");

        public static readonly ServiceError AccountLocked = new(
            new ServiceErrorCode(2, 4),
            "User account is temporarily locked.");

        public static readonly ServiceError EmailNotVerified = new(
            new ServiceErrorCode(2, 5),
            "Email address is not verified.");

        public static readonly ServiceError PlatformAdminRequired = new(
            new ServiceErrorCode(2, 6),
            "Platform administrator access is required.");

        public static readonly ServiceError CompanyAccessDenied = new(
            new ServiceErrorCode(2, 7),
            "User does not have access to the company.");

        public static readonly ServiceError CompanyRoleRequired = new(
            new ServiceErrorCode(2, 8),
            "Required company role is missing.");

        public static readonly ServiceError UserNotActive = new(
            new ServiceErrorCode(2, 9),
            "User is not active.");
    }
}

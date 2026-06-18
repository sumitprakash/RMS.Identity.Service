namespace RMS.Identity.Service.Application.Shared.Errors;

public static partial class ServiceErrorDefinitions
{
    public static class Users
    {
        public static readonly ServiceError UserNotFound = new(
            ServiceStatusErrorCodes.NotFound,
            new ServiceErrorCode(3, 1),
            "User could not be found.");

        public static readonly ServiceError UserExists = new(
            ServiceStatusErrorCodes.Conflict,
            new ServiceErrorCode(3, 2),
            "Email address already exists.");
    }
}

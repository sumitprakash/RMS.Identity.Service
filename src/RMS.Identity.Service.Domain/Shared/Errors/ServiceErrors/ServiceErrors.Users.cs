namespace RMS.Identity.Service.Domain.Shared.Errors;

public static partial class ServiceErrors
{
    public static class Users
    {
        public static readonly ServiceError UserNotFound = new(
            new ServiceErrorCode(3, 1),
            "User could not be found.");

        public static readonly ServiceError UserExists = new(
            new ServiceErrorCode(3, 2),
            "Email address already exists.");
    }
}

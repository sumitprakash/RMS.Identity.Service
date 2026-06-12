namespace RMS.Identity.Service.Application.Shared.Errors;

public static partial class ServiceErrors
{
    public static class General
    {
        public static readonly ServiceError UnhandledException = new(
            new ServiceErrorCode(1, 1),
            "An unexpected error occurred.");

        public static readonly ServiceError DatabaseTransactionMissing = new(
            new ServiceErrorCode(1, 2),
            "Database transaction is not available.");
    }
}

namespace RMS.Identity.Service.Application.Shared.Errors
{
    public class ConflictException : ServiceException
    {
        private const ServiceStatusErrorCodes statusCode = ServiceStatusErrorCodes.Conflict;

        public ConflictException(ServiceError error, object? details = null) : base(statusCode, error, details)
        {
        }

        public ConflictException(string message) : base(statusCode, message)
        {
        }
    }
}

namespace RMS.Identity.Service.Application.Shared.Errors
{
    public class ForbiddenException : ServiceException
    {
        private const ServiceStatusErrorCodes statusCode = ServiceStatusErrorCodes.Forbidden;

        public ForbiddenException(ServiceError error, object? details = null) : base(statusCode, error, details)
        {
        }

        public ForbiddenException(string message) : base(statusCode, message)
        {
        }
    }
}

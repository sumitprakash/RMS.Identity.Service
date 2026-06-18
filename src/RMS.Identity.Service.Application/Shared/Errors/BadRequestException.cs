namespace RMS.Identity.Service.Application.Shared.Errors
{
    public class BadRequestException : ServiceException
    {
        private const ServiceStatusErrorCodes statusCode = ServiceStatusErrorCodes.BadRequest;

        public BadRequestException(ServiceError error, object? details = null) : base(statusCode, error, details)
        {
        }

        public BadRequestException(string message) : base(statusCode, message)
        {
        }
    }
}

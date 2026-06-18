namespace RMS.Identity.Service.Application.Shared.Errors
{
    public class ResourceNotFoundException : ServiceException
    {
        private const ServiceStatusErrorCodes statusCode = ServiceStatusErrorCodes.NotFound;

        public ResourceNotFoundException(ServiceError error, object? details = null) : base(statusCode, error, details)
        {
        }

        public ResourceNotFoundException(string message) : base(statusCode, message)
        {
        }
    }
}

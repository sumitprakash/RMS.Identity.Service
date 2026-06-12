using RMS.Identity.Service.Domain.Shared.Errors;

namespace RMS.Identity.Service.Application.Shared.Errors
{
    public class ResourceNotFoundException : ServiceException
    {
        private const ServiceStatusErrorCodes statusCode = ServiceStatusErrorCodes.NotFound;

        public ResourceNotFoundException(ServiceError error, object? details) : base(statusCode, error, details)
        {
        }

        public ResourceNotFoundException(string message) : base(statusCode, message, null)
        {
        }
    }
}

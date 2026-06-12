using RMS.Identity.Service.Application.Shared.Errors;

namespace RMS.Identity.Service.Application.Shared.Errors
{
    public class UnauthorizedException : ServiceException
    {
        private const ServiceStatusErrorCodes statusCode = ServiceStatusErrorCodes.Unauthorized;

        public UnauthorizedException(ServiceError error, object? details) : base(statusCode, error, details)
        {
        }

        public UnauthorizedException(string message) : this(new ServiceError(null, message), null)
        {
        }
    }
}

using RMS.Identity.Service.Application.Shared.Errors;

namespace RMS.Identity.Service.Application.Shared.Errors
{
    public class InternalServerErrorException : ServiceException
    {
        private const ServiceStatusErrorCodes statusCode = ServiceStatusErrorCodes.InternalServerError;

        public InternalServerErrorException(ServiceError error, object? details) : base(statusCode, error, details)
        {
        }

        public InternalServerErrorException(string message) : base(statusCode, message, null)
        {
        }
    }
}

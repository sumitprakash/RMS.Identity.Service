namespace RMS.Identity.Service.Application.Shared.Errors;

public sealed class ApplicationServiceException : ServiceException
{
    public ApplicationServiceException(ServiceError error, object? details = null)
        : base(error, details)
    {
    }

    public ApplicationServiceException(ServiceStatusErrorCodes statusCode, string message, object? details = null)
        : base(statusCode, message, details)
    {
    }

    public ApplicationServiceException(string message, object? details = null)
        : base(ServiceStatusErrorCodes.InternalServerError, message, details)
    {
    }
}

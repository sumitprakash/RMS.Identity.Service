using RMS.Identity.Service.Application.Shared.Errors;

namespace RMS.Identity.Service.Application.Shared.Errors;

public abstract class ServiceException : Exception
{
    public ServiceStatusErrorCodes StatusCode { get; }

    public ServiceError? Error { get; }

    public object? Details { get; }
    
    public ServiceException(ServiceStatusErrorCodes statusCode, ServiceError error, object? details): base(error.Message)
    {
        this.StatusCode = statusCode;
        this.Error = error;
        this.Details = details;
    }

    public ServiceException(ServiceStatusErrorCodes statusCode, string message, object? details) : base(message)
    {
        this.StatusCode = statusCode;
        this.Error = new ServiceError(null, message);
        this.Details = details;
    }

    public string ToErrorResponseCode()
    {
        return $"{StatusCode}-{Error?.ServiceErrorMessage}";
    }
}

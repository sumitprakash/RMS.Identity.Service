using RMS.Identity.Service.Application.Shared.Errors;

namespace RMS.Identity.Service.Application.Shared.Errors;

public abstract class ServiceException : Exception
{
    public int StatusCode { get; }

    public ServiceStatusErrorCodes ExceptionType { get; }

    public ServiceError? Error { get; }

    public object? Details { get; }

    public string Code => ToErrorResponseCode();

    public ServiceErrorCode? ErrorCode => Error?.Code;

    public ServiceErrorCode? StructuredCode => ErrorCode;
    
    public ServiceException(ServiceStatusErrorCodes statusCode, ServiceError error, object? details): base(error.Message)
    {
        this.StatusCode = (int)statusCode;
        this.ExceptionType = statusCode;
        this.Error = error;
        this.Details = details;
    }

    public ServiceException(ServiceStatusErrorCodes statusCode, string message, object? details) : base(message)
    {
        this.StatusCode = (int)statusCode;
        this.ExceptionType = statusCode;
        this.Error = new ServiceError(null, message);
        this.Details = details;
    }

    public string ToErrorResponseCode()
    {
        return Error?.ToResponseCode(StatusCode) ?? StatusCode.ToString();
    }
}

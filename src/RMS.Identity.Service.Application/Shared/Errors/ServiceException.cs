namespace RMS.Identity.Service.Application.Shared.Errors;

public abstract class ServiceException : Exception
{
    public int StatusCode { get; }

    public ServiceError Error { get; }

    public object? Details { get; }

    public string Code => GetErrorCode();

    public ServiceException(ServiceStatusErrorCodes statusCode, ServiceError error, object? details): base(error.Message)
    {
        StatusCode = (int)statusCode;
        Error = error;
        Details = details;
    }

    public ServiceException(ServiceStatusErrorCodes statusCode, string message, object? details) : base(message)
    {
        StatusCode = (int)statusCode;
        Error = new ServiceError(null, message);
        Details = details;
    }

    private string GetErrorCode()
    {
        if (!Error.Code.HasValue)
        {
            return StatusCode.ToString();
        }

        return $"{StatusCode}-{Error.Code.Value.ErrorCode}";
    }
}

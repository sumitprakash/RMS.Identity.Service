namespace RMS.Identity.Service.Application.Shared.Errors;

public abstract class ServiceException : Exception
{
    public int StatusCode { get; }

    public ServiceError Error { get; }

    public object? Details { get; }

    public string Code => GetErrorCode();

    protected ServiceException(ServiceError error, object? details = null): base(error.Message)
    {
        StatusCode = (int)error.StatusCode;
        Error = error;
        Details = details;
    }

    protected ServiceException(ServiceStatusErrorCodes statusCode, string message, object? details = null)
        : this(new ServiceError(statusCode, null, message), details)
    {
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

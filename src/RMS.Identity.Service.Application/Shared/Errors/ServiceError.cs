namespace RMS.Identity.Service.Application.Shared.Errors;

public sealed class ServiceError
{
    public ServiceStatusErrorCodes StatusCode { get; }

    public ServiceErrorCode? Code { get; }

    public string Message { get; }

    public ServiceError(ServiceStatusErrorCodes statusCode, ServiceErrorCode? code, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Error message is required.", nameof(message));
        }

        StatusCode = statusCode;
        Code = code;
        Message = message;
    }
}

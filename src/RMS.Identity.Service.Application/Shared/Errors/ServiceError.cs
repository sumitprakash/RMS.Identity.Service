namespace RMS.Identity.Service.Application.Shared.Errors;

public sealed class ServiceError
{
    public ServiceErrorCode? Code { get; }

    public string Message { get; }

    public ServiceError(ServiceErrorCode? code, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Error message is required.", nameof(message));
        }

        Code = code;
        Message = message;
    }
}

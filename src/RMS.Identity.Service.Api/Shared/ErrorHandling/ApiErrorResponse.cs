using RMS.Identity.Service.Application.Shared.Errors;

namespace RMS.Identity.Service.Api.Shared.ErrorHandling;

public sealed record ApiErrorResponse(
    string ServiceCode,
    string Code,
    string Message,
    object? Details = null)
{
    public static ApiErrorResponse Create(string code, string message, object? details = null) =>
        new(ServiceIdentity.Code, code, message, details);

    public static ApiErrorResponse Create(ServiceError error, object? details = null)
    {
        var code = error.Code.HasValue
            ? $"{(int)error.StatusCode}-{error.Code.Value.ErrorCode}"
            : ((int)error.StatusCode).ToString();

        return new ApiErrorResponse(ServiceIdentity.Code, code, error.Message, details);
    }
}

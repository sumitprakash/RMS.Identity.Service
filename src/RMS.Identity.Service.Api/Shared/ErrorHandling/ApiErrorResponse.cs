using RMS.Identity.Service.Application.Shared.Errors;

namespace RMS.Identity.Service.Api.Shared.ErrorHandling;

public sealed record ApiErrorResponse(
    string Code,
    string Message,
    object? Details = null)
{
    public static ApiErrorResponse Create(string code, string message, object? details = null) =>
        new(code, message, details);

    public static ApiErrorResponse Create(ServiceError error, object? details = null)
    {
        var code = error.Code.HasValue
            ? $"{(int)error.StatusCode}-{error.Code.Value.ErrorCode}"
            : ((int)error.StatusCode).ToString();

        return new ApiErrorResponse(code, error.Message, details);
    }
}

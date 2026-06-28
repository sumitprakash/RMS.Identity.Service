using RMS.Identity.Service.Application.Shared.Errors;

namespace RMS.Identity.Service.Api.Shared.ErrorHandling;

public sealed record ApiErrorResponse(
    string ServiceCode,
    string Code,
    string Message,
    object? Details = null,
    string? CorrelationTraceId = null)
{
    public static ApiErrorResponse Create(
        string code,
        string message,
        object? details = null,
        string? correlationTraceId = null) =>
        new(ServiceIdentity.Code, code, message, details, correlationTraceId);

    public static ApiErrorResponse Create(
        ServiceError error,
        object? details = null,
        string? correlationTraceId = null)
    {
        var code = error.Code.HasValue
            ? $"{(int)error.StatusCode}-{error.Code.Value.ErrorCode}"
            : ((int)error.StatusCode).ToString();

        return new ApiErrorResponse(ServiceIdentity.Code, code, error.Message, details, correlationTraceId);
    }
}

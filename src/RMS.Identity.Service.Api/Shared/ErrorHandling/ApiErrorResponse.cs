namespace RMS.Identity.Service.Api.Shared.ErrorHandling;

public sealed record ApiErrorResponse(
    string Code,
    string Message,
    object? Details = null)
{
    public static ApiErrorResponse Create(string code, string message, object? details = null) =>
        new(code, message, details);
}

namespace RMS.Identity.Service.Api.Contracts;

public sealed record ErrorResponse(
    string Code,
    string Message,
    object? Details = null);

namespace RMS.Identity.Service.Application.Identity.Requests;

public sealed record SignUpUserCommand(
    string Username,
    string Password,
    string? DisplayName,
    string? Phone,
    string? IdempotencyKey);

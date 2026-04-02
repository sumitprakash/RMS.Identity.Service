namespace RMS.Identity.Service.Application.Logic.SignUp;

public sealed record SignUpCommand(
    string Username,
    string Password,
    string? DisplayName,
    string? Phone,
    string? IdempotencyKey);

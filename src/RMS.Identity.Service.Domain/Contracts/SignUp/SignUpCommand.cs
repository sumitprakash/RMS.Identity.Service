namespace RMS.Identity.Service.Domain.Contracts.SignUp;

public sealed record SignUpCommand(
    string Username,
    string Password,
    string? DisplayName,
    string? Phone,
    string? IdempotencyKey);

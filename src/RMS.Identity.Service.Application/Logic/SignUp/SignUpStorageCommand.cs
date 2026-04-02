namespace RMS.Identity.Service.Application.Logic.SignUp;

public sealed record SignUpStorageCommand(
    Guid UserUuid,
    string Username,
    string PasswordHash,
    string? DisplayName,
    string VerificationToken,
    string VerificationTokenHash,
    DateTime VerificationTokenExpiresAt,
    string? IdempotencyKey,
    string? RequestHash);

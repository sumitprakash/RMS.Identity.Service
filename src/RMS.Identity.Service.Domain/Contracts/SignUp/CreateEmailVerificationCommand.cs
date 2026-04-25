namespace RMS.Identity.Service.Domain.Contracts.SignUp;

public sealed record CreateEmailVerificationCommand(
    long UserId,
    string TokenHash,
    DateTime ExpiresAt);

namespace RMS.Identity.Service.Domain.Contracts.EmailVerification;

public sealed record CreateEmailVerificationCommand(
    long UserId,
    string TokenHash,
    DateTime ExpiresAt);

namespace RMS.Identity.Service.Domain.Contracts.VerifyEmail;

public sealed record CreateEmailVerificationCommand(
    long UserId,
    string TokenHash,
    string Purpose,
    DateTime ExpiresAt);

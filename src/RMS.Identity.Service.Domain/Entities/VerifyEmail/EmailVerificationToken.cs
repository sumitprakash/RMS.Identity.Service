namespace RMS.Identity.Service.Domain.Entities.VerifyEmail;

public sealed record EmailVerificationToken(
    long EmailVerificationId,
    long UserId,
    string TokenHash,
    string Purpose,
    DateTime ExpiresAt,
    bool Consumed);

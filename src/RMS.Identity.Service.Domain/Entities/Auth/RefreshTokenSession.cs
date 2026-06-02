namespace RMS.Identity.Service.Domain.Entities.Auth;

public sealed record RefreshTokenSession(
    long RefreshTokenId,
    string TokenHash,
    DateTime ExpiresAt,
    DateTime? RevokedAt,
    AuthenticatedUser User);

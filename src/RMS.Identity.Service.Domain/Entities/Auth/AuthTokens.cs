namespace RMS.Identity.Service.Domain.Entities.Auth;

public sealed record AuthTokens(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    DateTime RefreshTokenExpiresAt);

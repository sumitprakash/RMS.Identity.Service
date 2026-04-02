namespace RMS.Identity.Service.Application.Identity.Results;

public sealed record RefreshTokenResult(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn);

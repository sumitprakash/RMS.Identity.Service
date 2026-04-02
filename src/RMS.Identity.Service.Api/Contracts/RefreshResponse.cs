namespace RMS.Identity.Service.Api.Contracts;

public sealed record RefreshResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn);

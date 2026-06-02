namespace RMS.Identity.Service.Domain.Contracts.Refresh;

public sealed record RefreshCommandResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn);

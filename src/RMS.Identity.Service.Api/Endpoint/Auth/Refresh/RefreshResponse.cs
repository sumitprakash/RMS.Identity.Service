namespace RMS.Identity.Service.Api.Endpoint.Auth.Refresh;

public sealed record RefreshResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn);

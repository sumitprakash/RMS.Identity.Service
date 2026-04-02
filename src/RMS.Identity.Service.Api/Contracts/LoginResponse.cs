namespace RMS.Identity.Service.Api.Contracts;

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType,
    UserResponse User);

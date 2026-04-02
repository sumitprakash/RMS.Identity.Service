namespace RMS.Identity.Service.Application.Identity.Results;

public sealed record LoginResult(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType,
    UserResult User);

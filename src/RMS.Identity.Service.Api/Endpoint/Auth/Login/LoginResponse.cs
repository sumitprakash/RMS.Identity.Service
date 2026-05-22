namespace RMS.Identity.Service.Api.Endpoint.Auth.Login;

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType,
    LoginUserResponse User);

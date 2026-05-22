namespace RMS.Identity.Service.Domain.Contracts.Login;

public sealed record LoginCommandResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType,
    LoginUserCommandResponse User);

public sealed record LoginUserCommandResponse(
    Guid UserUuid,
    string Username,
    string? DisplayName,
    IReadOnlyCollection<string> Roles,
    string Status,
    DateTime CreatedAt);

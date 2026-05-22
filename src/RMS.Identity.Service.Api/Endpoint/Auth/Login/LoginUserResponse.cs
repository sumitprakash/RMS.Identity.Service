namespace RMS.Identity.Service.Api.Endpoint.Auth.Login;

public sealed record LoginUserResponse(
    Guid UserUuid,
    string Username,
    string? DisplayName,
    IReadOnlyCollection<string> Roles,
    string Status,
    DateTime CreatedAt);

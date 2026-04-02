namespace RMS.Identity.Service.Api.Contracts;

public sealed record UserResponse(
    Guid UserUuid,
    string Username,
    string? DisplayName,
    IReadOnlyList<string> Roles,
    string Status,
    DateTime CreatedAt);

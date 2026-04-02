namespace RMS.Identity.Service.Application.Identity.Results;

public sealed record UserResult(
    Guid UserUuid,
    string Username,
    string? DisplayName,
    IReadOnlyList<string> Roles,
    string Status,
    DateTime CreatedAt);

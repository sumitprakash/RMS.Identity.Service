namespace RMS.Identity.Service.Api.Contracts;

public sealed record CreateTenantUserRequest(
    string Username,
    string? DisplayName,
    IReadOnlyList<string>? Roles);

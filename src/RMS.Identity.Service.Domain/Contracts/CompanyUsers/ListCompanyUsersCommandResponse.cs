namespace RMS.Identity.Service.Domain.Contracts.CompanyUsers;

public sealed record ListCompanyUsersCommandResponse(
    IReadOnlyCollection<CompanyUserResponseItem> Users);

public sealed record CompanyUserResponseItem(
    Guid UserUuid,
    string Username,
    string? DisplayName,
    IReadOnlyCollection<string> Roles,
    string CompanyRole,
    string Status,
    DateTime CreatedAt);

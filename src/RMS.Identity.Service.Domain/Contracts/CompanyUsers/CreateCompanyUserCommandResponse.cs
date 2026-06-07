namespace RMS.Identity.Service.Domain.Contracts.CompanyUsers;

public sealed record CreateCompanyUserCommandResponse(
    Guid UserUuid,
    string Username,
    string? DisplayName,
    IReadOnlyCollection<string> Roles,
    string CompanyRole,
    string Status,
    DateTime CreatedAt);

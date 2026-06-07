namespace RMS.Identity.Service.Api.Endpoint.Companies.GetCompanyUser;

public sealed record UserResponse(
    Guid UserUuid,
    string Username,
    string? DisplayName,
    IReadOnlyCollection<string> Roles,
    string? CompanyRole,
    string Status,
    DateTime CreatedAt);

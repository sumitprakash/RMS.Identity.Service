namespace RMS.Identity.Service.Domain.Entities.Companies;

public sealed record CompanyUserAccount(
    Guid UserUuid,
    string Username,
    string? DisplayName,
    bool EmailVerified,
    bool IsActive,
    string CompanyRole,
    string MembershipStatus,
    DateTime CreatedAt);

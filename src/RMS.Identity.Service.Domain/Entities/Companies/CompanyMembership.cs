namespace RMS.Identity.Service.Domain.Entities.Companies;

public sealed record CompanyMembership(
    Guid UserUuid,
    Guid CompanyUuid,
    string CompanyStatus,
    string CompanyRole,
    string MembershipStatus);

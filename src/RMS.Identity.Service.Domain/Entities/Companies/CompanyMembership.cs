namespace RMS.Identity.Service.Domain.Entities.Companies;

public sealed record CompanyMembership(
    Guid UserUuid,
    Guid CompanyUuid,
    string CompanyRole,
    string MembershipStatus);

namespace RMS.Identity.Service.Domain.Contracts.CompanyUsers;

public sealed record UpdateCompanyUserCommand(
    Guid CompanyUuid,
    Guid UserUuid,
    string CompanyRole,
    string MembershipStatus);

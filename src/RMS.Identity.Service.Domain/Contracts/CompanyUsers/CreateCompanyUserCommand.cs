namespace RMS.Identity.Service.Domain.Contracts.CompanyUsers;

public sealed record CreateCompanyUserCommand(
    long CompanyId,
    long UserId,
    string CompanyRole,
    string MembershipStatus);

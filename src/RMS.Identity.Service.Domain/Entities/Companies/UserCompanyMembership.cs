namespace RMS.Identity.Service.Domain.Entities.Companies;

public sealed record UserCompanyMembership(
    Guid CompanyUuid,
    string LegalName,
    string? TradeName,
    string Gstin,
    string Status,
    string CompanyRole,
    string MembershipStatus,
    DateTime CreatedAt);

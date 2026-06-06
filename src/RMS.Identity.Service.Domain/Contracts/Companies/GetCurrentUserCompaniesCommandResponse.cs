namespace RMS.Identity.Service.Domain.Contracts.Companies;

public sealed record GetCurrentUserCompaniesCommandResponse(
    IReadOnlyCollection<UserCompanyCommandResponse> Companies);

public sealed record UserCompanyCommandResponse(
    Guid CompanyUuid,
    string LegalName,
    string? TradeName,
    string Gstin,
    string Status,
    string CompanyRole,
    string MembershipStatus,
    DateTime CreatedAt);

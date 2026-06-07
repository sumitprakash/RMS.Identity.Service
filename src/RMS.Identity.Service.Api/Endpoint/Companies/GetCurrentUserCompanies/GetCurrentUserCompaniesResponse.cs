namespace RMS.Identity.Service.Api.Endpoint.Companies.GetCurrentUserCompanies;

public sealed record CurrentUserCompaniesResponse(
    IReadOnlyCollection<CurrentUserCompanyResponse> Companies);

public sealed record CurrentUserCompanyResponse(
    Guid CompanyUuid,
    string LegalName,
    string? TradeName,
    string Gstin,
    string Status,
    string CompanyRole,
    string MembershipStatus,
    DateTime CreatedAt);

namespace RMS.Identity.Service.Api.Endpoint.Companies;

public sealed record RegisterCompanyResponse(
    Guid CompanyUuid,
    string LegalName,
    string? TradeName,
    string Gstin,
    string Status,
    DateTime CreatedAt);

public sealed record MyCompaniesResponse(
    IReadOnlyCollection<MyCompanyResponse> Companies);

public sealed record MyCompanyResponse(
    Guid CompanyUuid,
    string LegalName,
    string? TradeName,
    string Gstin,
    string Status,
    string CompanyRole,
    string MembershipStatus,
    DateTime CreatedAt);

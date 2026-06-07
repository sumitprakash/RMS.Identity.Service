namespace RMS.Identity.Service.Api.Endpoint.Companies.RegisterCompany;

public sealed record RegisterCompanyResponse(
    Guid CompanyUuid,
    string LegalName,
    string? TradeName,
    string Gstin,
    string Status,
    DateTime CreatedAt);

namespace RMS.Identity.Service.Api.Endpoint.Companies.UpdateCompany;

public sealed record CompanyResponse(
    Guid CompanyUuid,
    string? CompanyCode,
    string LegalName,
    string? TradeName,
    string Gstin,
    string ContactEmailAddress,
    string ContactPhoneNumber,
    RegisteredAddress RegisteredAddress,
    string Status);

public sealed record RegisteredAddress(
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode,
    string Country);

namespace RMS.Identity.Service.Api.Endpoint.Companies.GetCompany;

public sealed record CompanyResponse(
    Guid CompanyUuid,
    string? CompanyCode,
    string LegalName,
    string? TradeName,
    string Gstin,
    string ContactEmailAddress,
    string ContactPhoneNumber,
    RegisteredAddressResponse RegisteredAddress,
    string Status);

public sealed record RegisteredAddressResponse(
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode,
    string Country);

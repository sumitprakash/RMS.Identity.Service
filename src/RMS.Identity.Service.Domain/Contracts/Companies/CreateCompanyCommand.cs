namespace RMS.Identity.Service.Domain.Contracts.Companies;

public sealed record CreateCompanyCommand(
    Guid CompanyUuid,
    string LegalName,
    string? TradeName,
    string Gstin,
    string ContactEmailAddress,
    string ContactPhoneNumber,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode,
    string Country,
    string Status);

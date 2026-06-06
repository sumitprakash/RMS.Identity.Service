namespace RMS.Identity.Service.Domain.Entities.Companies;

public sealed record Company(
    long CompanyId,
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
    string Status,
    bool IsDeleted,
    DateTime CreatedAt);

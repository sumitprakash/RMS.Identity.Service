namespace RMS.Identity.Service.Domain.Contracts.Companies;

public sealed record RegisterCompanyCommandResponse(
    Guid CompanyUuid,
    string LegalName,
    string? TradeName,
    string Gstin,
    string Status,
    DateTime CreatedAt);

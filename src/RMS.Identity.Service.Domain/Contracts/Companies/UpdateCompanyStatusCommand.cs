namespace RMS.Identity.Service.Domain.Contracts.Companies;

public sealed record UpdateCompanyStatusCommand(
    Guid CompanyUuid,
    string ExpectedStatus,
    string Status);

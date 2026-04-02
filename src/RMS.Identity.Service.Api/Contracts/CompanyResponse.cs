namespace RMS.Identity.Service.Api.Contracts;

public sealed record CompanyResponse(
    Guid CompanyUuid,
    string? CompanyCode,
    string CompanyName,
    string? CompanyGstin);

namespace RMS.Identity.Service.Application.Identity.Results;

public sealed record CompanyResult(
    Guid CompanyUuid,
    string? CompanyCode,
    string CompanyName,
    string? CompanyGstin);

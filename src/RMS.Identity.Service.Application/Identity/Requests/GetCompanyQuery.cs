namespace RMS.Identity.Service.Application.Identity.Requests;

public sealed record GetCompanyQuery(
    Guid CompanyUuid,
    Guid? RequestingCompanyUuid);

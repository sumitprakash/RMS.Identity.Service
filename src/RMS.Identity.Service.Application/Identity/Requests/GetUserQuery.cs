namespace RMS.Identity.Service.Application.Identity.Requests;

public sealed record GetUserQuery(
    Guid UserUuid,
    Guid? RequestingCompanyUuid);

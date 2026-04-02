namespace RMS.Identity.Service.Application.Identity.Requests;

public sealed record LoginCommand(
    string Username,
    string Password,
    Guid? CompanyUuid);

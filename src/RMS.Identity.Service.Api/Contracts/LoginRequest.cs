namespace RMS.Identity.Service.Api.Contracts;

public sealed record LoginRequest(
    string Username,
    string Password,
    Guid? CompanyUuid);

using RMS.Identity.Service.Infrastructure.Cqrs;

namespace RMS.Identity.Service.Domain.Contracts.Login;

public sealed record LoginCommandRequest(
    string Username,
    string Password,
    Guid? CompanyUuid) : ICommand<LoginCommandResponse>;

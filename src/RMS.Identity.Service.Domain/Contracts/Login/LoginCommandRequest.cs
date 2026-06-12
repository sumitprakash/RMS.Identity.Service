using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Domain.Contracts.Login;

public sealed record LoginCommandRequest(
    string Username,
    string Password) : ICommand<LoginCommandResponse>;

using RMS.Identity.Service.Infrastructure.Cqrs;

namespace RMS.Identity.Service.Domain.Contracts.VerifyEmail;

public sealed record VerifyEmailCommandRequest(
    string Token) : ICommand<VerifyEmailCommandResponse>;

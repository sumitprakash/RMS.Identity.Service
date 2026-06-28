using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Domain.Contracts.VerifyEmail;

public sealed record VerifyEmailCommandRequest(
    string Token,
    string? Password = null) : ICommand<VerifyEmailCommandResponse>;

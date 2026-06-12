using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Domain.Contracts.Refresh;

public sealed record RefreshCommandRequest(
    string RefreshToken) : ICommand<RefreshCommandResponse>;

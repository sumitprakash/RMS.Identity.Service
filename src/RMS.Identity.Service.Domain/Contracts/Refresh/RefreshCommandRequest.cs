using RMS.Identity.Service.Infrastructure.Cqrs;

namespace RMS.Identity.Service.Domain.Contracts.Refresh;

public sealed record RefreshCommandRequest(
    string RefreshToken) : ICommand<RefreshCommandResponse>;

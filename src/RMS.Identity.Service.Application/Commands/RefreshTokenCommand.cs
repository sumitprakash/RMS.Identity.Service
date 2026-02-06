using MediatR;

namespace RMS.Identity.Service.Application.Commands
{
    public sealed class RefreshTokenCommand : ITransactionalCommand, IRequest<RMS.Identity.Service.Application.DTOs.AuthResult>
    {
        public string RawRefreshToken { get; init; } = null!;
        public Guid RequestCorrelationId { get; init; } = Guid.NewGuid();
    }
}

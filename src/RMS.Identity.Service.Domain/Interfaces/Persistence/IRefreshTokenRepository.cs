using RMS.Identity.Service.Domain.Entities;

namespace RMS.Identity.Service.Domain.Interfaces.Persistence;

public interface IRefreshTokenRepository
{
    Task CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task RevokeAsync(long refreshTokenId, string? replacedByTokenHash, CancellationToken cancellationToken = default);
}

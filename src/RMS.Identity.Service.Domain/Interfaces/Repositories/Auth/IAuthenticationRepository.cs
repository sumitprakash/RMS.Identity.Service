using RMS.Identity.Service.Domain.Entities.Auth;

namespace RMS.Identity.Service.Domain.Interfaces.Repositories.Auth;

public interface IAuthenticationRepository
{
    Task<AuthenticatedUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken);

    Task<RefreshTokenSession?> GetRefreshTokenSessionAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken);

    Task RecordFailedLoginAsync(long userId, CancellationToken cancellationToken);

    Task RecordSuccessfulLoginAsync(
        long userId,
        string refreshTokenHash,
        DateTime refreshTokenExpiresAt,
        CancellationToken cancellationToken);

    Task<bool> RotateRefreshTokenAsync(
        long refreshTokenId,
        long userId,
        string newRefreshTokenHash,
        DateTime newRefreshTokenExpiresAt,
        CancellationToken cancellationToken);
}

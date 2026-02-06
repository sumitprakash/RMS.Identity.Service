using RMS.Identity.Service.Domain.Entities;

namespace RMS.Identity.Service.Application.Repositories
{
    public interface IRefreshTokenRepository
    {
        Task SaveAsync(Domain.Entities.RefreshToken refreshToken);
        Task<Domain.Entities.RefreshToken?> GetByTokenHashAsync(string tokenHash);
        Task RevokeAsync(long refreshTokenId, string? replacedByTokenHash = null);
        Task<RefreshToken> GetByHashAsync(string hashed);
        RefreshToken CreateForUser(long userID);
    }
}
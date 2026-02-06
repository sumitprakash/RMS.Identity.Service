namespace RMS.Identity.Service.Infrastructure.Repositories
{
    public interface IRefreshTokenRepository
    {
        Task SaveAsync(RMS.Identity.Service.Domain.Entities.RefreshToken refreshToken);
        Task<RMS.Identity.Service.Domain.Entities.RefreshToken?> GetByTokenHashAsync(string tokenHash);
        Task RevokeAsync(long refreshTokenId, string? replacedByTokenHash = null);
    }
}

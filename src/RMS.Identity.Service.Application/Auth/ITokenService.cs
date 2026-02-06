namespace RMS.Identity.Service.Application.Auth
{
    public interface ITokenService
    {
        string CreateAccessToken(Guid userUuid, Guid companyUuid, IEnumerable<string> roles);
        Task<(string rawRefreshToken, string tokenHash)> CreateAndStoreRefreshTokenAsync(long userId);
        Task<(string accessToken, string refreshToken)?> RefreshAsync(string rawRefreshToken);
        Task RevokeRefreshTokenAsync(string rawRefreshToken);
    }
}

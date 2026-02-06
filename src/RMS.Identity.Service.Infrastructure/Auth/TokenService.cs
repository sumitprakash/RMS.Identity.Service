using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using RMS.Identity.Service.Infrastructure.Repositories;

namespace RMS.Identity.Service.Infrastructure.Auth
{
    public class TokenService : ITokenService
    {
        private readonly JwtOptions _opts;
        private readonly IRefreshTokenRepository _refreshRepo;
        private readonly IUserRepository _userRepo;
        private readonly IUserRoleRepository _userRoleRepo;
        private readonly ICompanyRepository _companyRepo;

        public TokenService(IOptions<JwtOptions> opts,
                            IRefreshTokenRepository refreshRepo,
                            IUserRepository userRepo,
                            IUserRoleRepository userRoleRepo,
                            ICompanyRepository companyRepo)
        {
            _opts = opts.Value;
            _refreshRepo = refreshRepo;
            _userRepo = userRepo;
            _userRoleRepo = userRoleRepo;
            _companyRepo = companyRepo;
        }

        private SymmetricSecurityKey GetSigningKey()
        {
            var keyRaw = Environment.GetEnvironmentVariable(_opts.SigningKeyEnvVar)
                         ?? throw new InvalidOperationException("JWT signing key env var missing");
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyRaw));
        }

        public string CreateAccessToken(Guid userUuid, Guid companyUuid, IEnumerable<string> roles)
        {
            var creds = new SigningCredentials(GetSigningKey(), SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userUuid.ToString()),
                new Claim("userUuid", userUuid.ToString()),
                new Claim("companyUuid", companyUuid.ToString())
            };
            foreach (var r in roles) claims.Add(new Claim(ClaimTypes.Role, r));

            var token = new JwtSecurityToken(
                issuer: _opts.Issuer,
                audience: _opts.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_opts.AccessTokenMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<(string rawRefreshToken, string tokenHash)> CreateAndStoreRefreshTokenAsync(long userId)
        {
            var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            using var sha = SHA256.Create();
            var hash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));

            var rt = new RMS.Identity.Service.Domain.Entities.RefreshToken
            {
                UserID = userId,
                TokenHash = hash,
                ExpiresAt = DateTime.UtcNow.AddDays(_opts.RefreshTokenDays),
                CreatedAt = DateTime.UtcNow
            };

            await _refreshRepo.SaveAsync(rt);
            return (raw, hash);
        }

        public async Task<(string accessToken, string refreshToken)?> RefreshAsync(string rawRefreshToken)
        {
            using var sha = SHA256.Create();
            var hash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(rawRefreshToken)));

            var existing = await _refreshRepo.GetByTokenHashAsync(hash);
            if (existing == null) return null;
            if (existing.ExpiresAt < DateTime.UtcNow || existing.RevokedAt != null) return null;

            // revoke old token
            await _refreshRepo.RevokeAsync(existing.RefreshTokenID);

            // create and store new refresh token
            var (raw, newHash) = await CreateAndStoreRefreshTokenAsync(existing.UserID);

            // load user by internal id and its roles
            var user = await _userRepo.GetByIdAsync(existing.UserID);
            if (user == null) return null;

            var roles = (await _userRoleRepo.GetRolesForUserAsync(user.UserID)).Select(r => r.Name).ToArray();

            // load company for uuid
            var company = await _companyRepo.GetByIdAsync(user.CompanyID);
            if (company == null) return null;
            var access = CreateAccessToken(user.UserUUID, company.CompanyUUID, roles);

            return (accessToken: access, refreshToken: raw);
        }

        public async Task RevokeRefreshTokenAsync(string rawRefreshToken)
        {
            using var sha = SHA256.Create();
            var hash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(rawRefreshToken)));
            var existing = await _refreshRepo.GetByTokenHashAsync(hash);
            if (existing != null) await _refreshRepo.RevokeAsync(existing.RefreshTokenID);
        }
    }
}

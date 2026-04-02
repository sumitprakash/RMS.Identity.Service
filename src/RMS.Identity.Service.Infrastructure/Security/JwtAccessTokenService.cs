using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RMS.Identity.Service.Domain.Interfaces.Security;
using RMS.Identity.Service.Domain.Models;

namespace RMS.Identity.Service.Infrastructure.Security;

internal sealed class JwtAccessTokenService : IAccessTokenService
{
    private readonly JwtOptions _options;

    public JwtAccessTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public AccessTokenResult Create(Guid userUuid, Guid? companyUuid, IReadOnlyCollection<string> roles)
    {
        var nowUtc = DateTime.UtcNow;
        var expiresAt = nowUtc.AddMinutes(_options.AccessTokenMinutes);
        var credentials = new SigningCredentials(GetSigningKey(), SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userUuid.ToString()),
            new("userUuid", userUuid.ToString())
        };

        if (companyUuid.HasValue)
        {
            claims.Add(new Claim("companyUuid", companyUuid.Value.ToString()));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("roles", role));
        }

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: nowUtc,
            expires: expiresAt,
            signingCredentials: credentials);

        return new AccessTokenResult(
            new JwtSecurityTokenHandler().WriteToken(token),
            (int)(expiresAt - nowUtc).TotalSeconds);
    }

    private SymmetricSecurityKey GetSigningKey()
    {
        var signingKey = _options.SigningKey;
        if (string.IsNullOrWhiteSpace(signingKey) && !string.IsNullOrWhiteSpace(_options.SigningKeyEnvVar))
        {
            signingKey = Environment.GetEnvironmentVariable(_options.SigningKeyEnvVar);
        }

        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException("JWT signing key is not configured");
        }

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
    }
}

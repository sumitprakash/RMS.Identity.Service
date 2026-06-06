using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RMS.Identity.Service.Domain.Entities.Auth;
using RMS.Identity.Service.Domain.Interfaces.Security;

namespace RMS.Identity.Service.Infrastructure.Security;

public sealed class JwtAuthTokenGenerator : IAuthTokenGenerator
{
    private readonly JwtOptions _options;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;

    public JwtAuthTokenGenerator(
        IOptions<JwtOptions> options,
        IRefreshTokenGenerator refreshTokenGenerator)
    {
        _options = options.Value;
        _refreshTokenGenerator = refreshTokenGenerator;
    }

    public AuthTokens Generate(AuthenticatedUser user)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddSeconds(_options.AccessTokenLifetimeSeconds);
        var accessToken = CreateAccessToken(user, now, expiresAt);
        var refreshToken = _refreshTokenGenerator.Generate();

        return new AuthTokens(
            accessToken,
            refreshToken,
            _options.AccessTokenLifetimeSeconds,
            DateTime.UtcNow.AddDays(_options.RefreshTokenLifetimeDays));
    }

    private string CreateAccessToken(AuthenticatedUser user, DateTimeOffset issuedAt, DateTimeOffset expiresAt)
    {
        var header = new Dictionary<string, object>
        {
            ["alg"] = "HS256",
            ["typ"] = "JWT"
        };
        var payload = new Dictionary<string, object?>
        {
            ["iss"] = _options.Issuer,
            ["aud"] = _options.Audience,
            ["sub"] = user.UserUuid.ToString(),
            ["jti"] = Guid.NewGuid().ToString(),
            ["username"] = user.Username,
            ["iat"] = issuedAt.ToUnixTimeSeconds(),
            ["nbf"] = issuedAt.ToUnixTimeSeconds(),
            ["exp"] = expiresAt.ToUnixTimeSeconds()
        };

        var encodedHeader = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(header));
        var encodedPayload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload));
        var unsignedToken = $"{encodedHeader}.{encodedPayload}";
        var signature = Sign(unsignedToken);

        return $"{unsignedToken}.{signature}";
    }

    private string Sign(string unsignedToken)
    {
        var signingKey = Encoding.UTF8.GetBytes(_options.SigningKey);
        if (signingKey.Length < 32)
        {
            throw new InvalidOperationException("JWT signing key must be at least 32 bytes.");
        }

        using var hmac = new HMACSHA256(signingKey);
        return Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(unsignedToken)));
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}

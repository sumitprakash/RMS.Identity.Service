using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Infrastructure.Security;

namespace RMS.Identity.Service.Api.Shared.Auth;

public sealed class JwtAccessTokenUserResolver : IAccessTokenUserResolver
{
    private readonly JwtOptions _options;

    public JwtAccessTokenUserResolver(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public Guid ResolveRequiredUserUuid(HttpContext context)
    {
        var authenticatedUserUuid = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(authenticatedUserUuid, out var resolvedUserUuid))
        {
            return resolvedUserUuid;
        }

        var authorizationHeader = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorizationHeader)
            || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            throw Unauthorized("Authorization bearer token is required.");
        }

        var token = authorizationHeader["Bearer ".Length..].Trim();
        var parts = token.Split('.');
        if (parts.Length != 3 || parts.Any(string.IsNullOrWhiteSpace))
        {
            throw Unauthorized("Authorization bearer token is invalid.");
        }

        try
        {
            using var header = JsonDocument.Parse(Base64UrlDecode(parts[0]));
            if (!StringClaimEquals(header.RootElement, "alg", "HS256")
                || !StringClaimEquals(header.RootElement, "typ", "JWT"))
            {
                throw Unauthorized("Authorization bearer token is invalid.");
            }

            var unsignedToken = $"{parts[0]}.{parts[1]}";
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(Sign(unsignedToken)),
                    Encoding.ASCII.GetBytes(parts[2])))
            {
                throw Unauthorized("Authorization bearer token is invalid.");
            }

            using var payload = JsonDocument.Parse(Base64UrlDecode(parts[1]));
            var root = payload.RootElement;

            if (!StringClaimEquals(root, "iss", _options.Issuer)
                || !StringClaimEquals(root, "aud", _options.Audience))
            {
                throw Unauthorized("Authorization bearer token is invalid.");
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (!TryGetLongClaim(root, "exp", out var expiresAt) || expiresAt <= now)
            {
                throw Unauthorized("Authorization bearer token is expired.");
            }

            if (TryGetLongClaim(root, "nbf", out var notBefore) && notBefore > now)
            {
                throw Unauthorized("Authorization bearer token is not active yet.");
            }

            if (!root.TryGetProperty("sub", out var subClaim)
                || subClaim.ValueKind != JsonValueKind.String
                || !Guid.TryParse(subClaim.GetString(), out var userUuid))
            {
                throw Unauthorized("Authorization bearer token is invalid.");
            }

            return userUuid;
        }
        catch (ServiceException)
        {
            throw;
        }
        catch
        {
            throw Unauthorized("Authorization bearer token is invalid.");
        }
    }

    private string Sign(string unsignedToken)
    {
        var signingKey = Encoding.UTF8.GetBytes(_options.SigningKey);
        using var hmac = new HMACSHA256(signingKey);
        return Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(unsignedToken)));
    }

    private static bool StringClaimEquals(JsonElement root, string claimName, string expected) =>
        root.TryGetProperty(claimName, out var claim)
        && claim.ValueKind == JsonValueKind.String
        && string.Equals(claim.GetString(), expected, StringComparison.Ordinal);

    private static bool TryGetLongClaim(JsonElement root, string claimName, out long value)
    {
        value = default;
        return root.TryGetProperty(claimName, out var claim)
            && claim.ValueKind == JsonValueKind.Number
            && claim.TryGetInt64(out value);
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
        return Convert.FromBase64String(base64);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static ServiceException Unauthorized(string message) =>
        new ApplicationServiceException(ServiceStatusErrorCodes.Unauthorized, message);
}

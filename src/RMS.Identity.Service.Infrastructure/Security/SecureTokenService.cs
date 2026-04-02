using System.Security.Cryptography;
using System.Text;
using RMS.Identity.Service.Domain.Interfaces.Security;

namespace RMS.Identity.Service.Infrastructure.Security;

internal sealed class SecureTokenService : ISecureTokenService
{
    public string GenerateToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    }

    public string HashToken(string rawToken)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
    }
}

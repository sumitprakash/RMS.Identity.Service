using System.Security.Cryptography;
using System.Text;
using RMS.Identity.Service.Domain.Interfaces.Security;

namespace RMS.Identity.Service.Infrastructure.Security;

public sealed class Sha256TextHasher : ITextHasher
{
    public string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

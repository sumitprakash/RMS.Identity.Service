using RMS.Identity.Service.Domain.Interfaces.Security;

namespace RMS.Identity.Service.Infrastructure.Security;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string value) => BCrypt.Net.BCrypt.HashPassword(value);
}

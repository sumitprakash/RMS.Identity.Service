namespace RMS.Identity.Service.Domain.Interfaces.Security;

public interface ISecureTokenService
{
    string GenerateToken();

    string HashToken(string rawToken);
}

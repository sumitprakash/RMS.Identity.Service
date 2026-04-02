namespace RMS.Identity.Service.Domain.Interfaces.Security;

public interface IPasswordHasher
{
    string Hash(string value);
}

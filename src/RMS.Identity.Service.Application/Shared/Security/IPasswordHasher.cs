namespace RMS.Identity.Service.Application.Shared.Security;

public interface IPasswordHasher
{
    string Hash(string value);
}

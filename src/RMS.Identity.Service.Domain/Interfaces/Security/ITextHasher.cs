namespace RMS.Identity.Service.Domain.Interfaces.Security;

public interface ITextHasher
{
    string Hash(string value);
}

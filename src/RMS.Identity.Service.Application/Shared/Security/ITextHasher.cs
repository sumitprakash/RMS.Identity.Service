namespace RMS.Identity.Service.Application.Shared.Security;

public interface ITextHasher
{
    string Hash(string value);
}

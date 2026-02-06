
namespace RMS.Identity.Service.Application.Handlers
{
    public interface ITokenGenerator
    {
        string CreateAccessToken(Guid userUUID, object companyUUID, string[] roles);
    }
}
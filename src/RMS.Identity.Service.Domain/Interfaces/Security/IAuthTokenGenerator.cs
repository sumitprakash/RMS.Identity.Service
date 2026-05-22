using RMS.Identity.Service.Domain.Entities.Auth;

namespace RMS.Identity.Service.Domain.Interfaces.Security;

public interface IAuthTokenGenerator
{
    AuthTokens Generate(AuthenticatedUser user);
}

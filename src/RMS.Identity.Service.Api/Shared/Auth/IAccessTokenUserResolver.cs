namespace RMS.Identity.Service.Api.Shared.Auth;

public interface IAccessTokenUserResolver
{
    Guid ResolveRequiredUserUuid(HttpContext context);
}

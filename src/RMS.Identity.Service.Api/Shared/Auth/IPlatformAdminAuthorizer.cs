namespace RMS.Identity.Service.Api.Shared.Auth;

public interface IPlatformAdminAuthorizer
{
    Task AuthorizeAsync(
        Guid userUuid,
        CancellationToken cancellationToken);
}

namespace RMS.Identity.Service.Domain.Interfaces.Repositories.Roles;

public interface IOperationalRoleReadRepository
{
    Task<bool> UserHasAnyRoleAsync(
        Guid userUuid,
        IReadOnlyCollection<string> roleNames,
        CancellationToken cancellationToken);
}

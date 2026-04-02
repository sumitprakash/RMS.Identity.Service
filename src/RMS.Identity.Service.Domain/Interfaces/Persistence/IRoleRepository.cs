using RMS.Identity.Service.Domain.Entities;

namespace RMS.Identity.Service.Domain.Interfaces.Persistence;

public interface IRoleRepository
{
    Task<IReadOnlyList<Role>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);
}

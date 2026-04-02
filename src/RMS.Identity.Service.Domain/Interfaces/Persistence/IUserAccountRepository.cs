using RMS.Identity.Service.Domain.Entities;

namespace RMS.Identity.Service.Domain.Interfaces.Persistence;

public interface IUserAccountRepository
{
    Task<UserAccount?> GetByIdAsync(long userId, CancellationToken cancellationToken = default);

    Task<UserAccount?> GetByUuidAsync(Guid userUuid, CancellationToken cancellationToken = default);

    Task<UserAccount?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<long> CreateAsync(UserAccount user, CancellationToken cancellationToken = default);

    Task MarkEmailVerifiedAsync(long userId, CancellationToken cancellationToken = default);

    Task RecordSuccessfulLoginAsync(long userId, DateTime loginAtUtc, CancellationToken cancellationToken = default);

    Task RecordFailedLoginAsync(long userId, CancellationToken cancellationToken = default);
}

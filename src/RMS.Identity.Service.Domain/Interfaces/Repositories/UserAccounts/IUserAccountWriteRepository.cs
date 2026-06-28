using RMS.Identity.Service.Domain.Contracts.UserAccounts;

namespace RMS.Identity.Service.Domain.Interfaces.Repositories.UserAccounts;

public interface IUserAccountWriteRepository
{
    Task<long> CreateAsync(
        CreateUserAccountCommand command,
        CancellationToken cancellationToken);

    Task MarkEmailVerifiedAsync(
        long userId,
        CancellationToken cancellationToken);

    Task CompletePasswordSetupAsync(
        long userId,
        string passwordHash,
        CancellationToken cancellationToken);
}

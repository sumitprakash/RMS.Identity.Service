using RMS.Identity.Service.Domain.Contracts.UserAccounts;
using RMS.Identity.Service.Domain.Entities.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Persistence;

namespace RMS.Identity.Service.Domain.Interfaces.UserAccounts;

public interface IUserAccountRepository
{
    Task<bool> ExistsByUsernameAsync(
        IDatabaseTransaction transaction,
        string username,
        CancellationToken cancellationToken);

    Task<long> CreateAsync(
        IDatabaseTransaction transaction,
        CreateUserAccountCommand command,
        CancellationToken cancellationToken);

    Task<UserAccount> GetByIdAsync(
        IDatabaseTransaction transaction,
        long userId,
        CancellationToken cancellationToken);
}

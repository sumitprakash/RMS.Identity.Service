using RMS.Identity.Service.Domain.Contracts.UserAccounts;
using RMS.Identity.Service.Domain.Entities.UserAccounts;

namespace RMS.Identity.Service.Domain.Interfaces.Repositories.UserAccounts;

public interface IUserAccountRepository
{
    Task<bool> ExistsByUsernameAsync(
        string username,
        CancellationToken cancellationToken);

    Task<long> CreateAsync(
        CreateUserAccountCommand command,
        CancellationToken cancellationToken);

    Task<UserAccount> GetByIdAsync(
        long userId,
        CancellationToken cancellationToken);
}

using RMS.Identity.Service.Domain.Entities.UserAccounts;

namespace RMS.Identity.Service.Domain.Interfaces.Repositories.UserAccounts;

public interface IUserAccountReadRepository
{
    Task<bool> ExistsByUsernameAsync(
        string username,
        CancellationToken cancellationToken);

    Task<UserAccount> GetByIdAsync(
        long userId,
        CancellationToken cancellationToken);
}

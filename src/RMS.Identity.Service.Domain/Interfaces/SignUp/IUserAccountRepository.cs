using RMS.Identity.Service.Domain.Contracts.SignUp;
using RMS.Identity.Service.Domain.Entities.SignUp;
using RMS.Identity.Service.Domain.Interfaces.Persistence;

namespace RMS.Identity.Service.Domain.Interfaces.SignUp;

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

    Task<SignUpUser> GetSignUpUserAsync(
        IDatabaseTransaction transaction,
        long userId,
        CancellationToken cancellationToken);
}

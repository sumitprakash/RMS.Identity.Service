using RMS.Identity.Service.Domain.Contracts.SignUp;
using RMS.Identity.Service.Domain.Entities.SignUp;

namespace RMS.Identity.Service.Domain.Interfaces.SignUp;

public interface ISignUpStore
{
    Task<SignUpUser> ExecuteAsync(SignUpStorageCommand command, CancellationToken cancellationToken);
}

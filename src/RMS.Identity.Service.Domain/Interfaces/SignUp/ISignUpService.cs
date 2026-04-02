using RMS.Identity.Service.Domain.Contracts.SignUp;
using RMS.Identity.Service.Domain.Entities.SignUp;

namespace RMS.Identity.Service.Domain.Interfaces.SignUp;

public interface ISignUpService
{
    Task<SignUpUser> ExecuteAsync(SignUpCommand command, CancellationToken cancellationToken);
}

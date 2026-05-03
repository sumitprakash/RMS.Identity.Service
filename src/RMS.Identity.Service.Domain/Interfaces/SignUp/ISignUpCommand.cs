using RMS.Identity.Service.Domain.Contracts.SignUp;
using RMS.Identity.Service.Domain.Entities.SignUp;

namespace RMS.Identity.Service.Domain.Interfaces.SignUp;

public interface ISignUpCommand
{
    Task<SignUpCommandResponse> ExecuteAsync(SignUpCommandRequest command, CancellationToken cancellationToken);
}

using RMS.Identity.Service.Domain.Entities.SignUp;

namespace RMS.Identity.Service.Application.Logic.SignUp;

public interface ISignUpService
{
    Task<SignUpUser> ExecuteAsync(SignUpCommand command, CancellationToken cancellationToken);
}

using RMS.Identity.Service.Domain.Entities.SignUp;

namespace RMS.Identity.Service.Application.Logic.SignUp;

public interface ISignUpStore
{
    Task<SignUpUser> ExecuteAsync(SignUpStorageCommand command, CancellationToken cancellationToken);
}

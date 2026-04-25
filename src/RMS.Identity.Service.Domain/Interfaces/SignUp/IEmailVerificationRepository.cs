using RMS.Identity.Service.Domain.Contracts.SignUp;
using RMS.Identity.Service.Domain.Interfaces.Persistence;

namespace RMS.Identity.Service.Domain.Interfaces.SignUp;

public interface IEmailVerificationRepository
{
    Task CreateAsync(
        IDatabaseTransaction transaction,
        CreateEmailVerificationCommand command,
        CancellationToken cancellationToken);
}

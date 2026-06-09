using RMS.Identity.Service.Domain.Contracts.VerifyEmail;

namespace RMS.Identity.Service.Domain.Interfaces.Repositories.VerifyEmail;

public interface IEmailVerificationWriteRepository
{
    Task CreateAsync(
        CreateEmailVerificationCommand command,
        CancellationToken cancellationToken);

    Task ConsumeAsync(
        long emailVerificationId,
        CancellationToken cancellationToken);
}

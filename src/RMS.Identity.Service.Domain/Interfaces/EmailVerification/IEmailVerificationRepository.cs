using RMS.Identity.Service.Domain.Contracts.EmailVerification;
using RMS.Identity.Service.Domain.Interfaces.Persistence;

namespace RMS.Identity.Service.Domain.Interfaces.EmailVerification;

public interface IEmailVerificationRepository
{
    Task CreateAsync(
        IDatabaseTransaction transaction,
        CreateEmailVerificationCommand command,
        CancellationToken cancellationToken);
}

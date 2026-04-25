using RMS.Identity.Service.Domain.Contracts.SignUp;

namespace RMS.Identity.Service.Domain.Interfaces.SignUp;

public interface IVerificationEmailOutboxRepository
{
    Task EnqueueAsync(
        VerificationEmailOutboxMessage message,
        CancellationToken cancellationToken);
}

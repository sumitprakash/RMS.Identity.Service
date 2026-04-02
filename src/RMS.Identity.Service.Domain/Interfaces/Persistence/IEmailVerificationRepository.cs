using RMS.Identity.Service.Domain.Entities;

namespace RMS.Identity.Service.Domain.Interfaces.Persistence;

public interface IEmailVerificationRepository
{
    Task CreateAsync(EmailVerification verification, CancellationToken cancellationToken = default);

    Task<EmailVerification?> GetActiveByTokenHashAsync(string tokenHash, string purpose, CancellationToken cancellationToken = default);

    Task MarkConsumedAsync(long emailVerificationId, CancellationToken cancellationToken = default);
}

using RMS.Identity.Service.Domain.Entities.VerifyEmail;

namespace RMS.Identity.Service.Domain.Interfaces.Repositories.VerifyEmail;

public interface IEmailVerificationReadRepository
{
    Task<EmailVerificationToken?> GetByTokenHashAsync(
        string tokenHash,
        string purpose,
        CancellationToken cancellationToken);
}

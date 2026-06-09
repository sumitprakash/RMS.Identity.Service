using RMS.Identity.Service.Domain.Entities.UserAccounts;

namespace RMS.Identity.Service.Domain.Interfaces.Repositories.Outbox;

public interface IOutboxWriteRepository
{
    Task InsertEmailVerificationRequestedAsync(
        UserAccount account,
        string token,
        DateTime expiresAt,
        CancellationToken cancellationToken);
}

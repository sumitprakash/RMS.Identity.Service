using RMS.Identity.Service.Domain.Entities.UserAccounts;

namespace RMS.Identity.Service.Domain.Interfaces.Repositories.AuditLog;

public interface IAuditLogWriteRepository
{
    Task InsertSignUpCreatedAsync(
        UserAccount account,
        CancellationToken cancellationToken);
}

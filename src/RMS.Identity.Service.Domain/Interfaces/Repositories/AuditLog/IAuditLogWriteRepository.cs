using RMS.Identity.Service.Domain.Entities.UserAccounts;
using RMS.Identity.Service.Domain.Entities.Companies;

namespace RMS.Identity.Service.Domain.Interfaces.Repositories.AuditLog;

public interface IAuditLogWriteRepository
{
    Task InsertSignUpCreatedAsync(
        UserAccount account,
        CancellationToken cancellationToken);

    Task InsertCompanyStatusChangedAsync(
        Company company,
        string previousStatus,
        long actorUserId,
        CancellationToken cancellationToken);
}

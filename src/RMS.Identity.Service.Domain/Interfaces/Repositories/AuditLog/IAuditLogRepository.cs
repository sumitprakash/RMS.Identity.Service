using RMS.Identity.Service.Domain.Entities.SignUp;

namespace RMS.Identity.Service.Domain.Interfaces.Repositories.AuditLog;

public interface IAuditLogRepository
{
    Task InsertSignUpCreatedAsync(
        SignUpUser createdUser,
        CancellationToken cancellationToken);
}

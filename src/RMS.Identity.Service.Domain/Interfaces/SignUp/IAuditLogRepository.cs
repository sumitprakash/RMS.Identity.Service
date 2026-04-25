using RMS.Identity.Service.Domain.Entities.SignUp;
using RMS.Identity.Service.Domain.Interfaces.Persistence;

namespace RMS.Identity.Service.Domain.Interfaces.SignUp;

public interface IAuditLogRepository
{
    Task InsertSignUpCreatedAsync(
        IDatabaseTransaction transaction,
        SignUpUser createdUser,
        CancellationToken cancellationToken);
}

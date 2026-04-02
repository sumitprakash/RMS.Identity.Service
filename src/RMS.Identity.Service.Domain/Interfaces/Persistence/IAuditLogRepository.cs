using RMS.Identity.Service.Domain.Entities;

namespace RMS.Identity.Service.Domain.Interfaces.Persistence;

public interface IAuditLogRepository
{
    Task CreateAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
}

using Dapper;
using RMS.Identity.Service.Domain.Entities;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Infrastructure.Persistence.Internal;

namespace RMS.Identity.Service.Infrastructure.Persistence;

internal sealed class AuditLogRepository : RepositoryBase, IAuditLogRepository
{
    public AuditLogRepository(MySqlConnectionFactory connectionFactory, DbSessionAccessor sessionAccessor)
        : base(connectionFactory, sessionAccessor)
    {
    }

    public Task CreateAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO AuditLog
            (
                TableName,
                RecordId,
                Action,
                ActorUserID,
                Payload,
                CreatedAt
            )
            VALUES
            (
                @TableName,
                @RecordId,
                @Action,
                @ActorUserID,
                @PayloadJson,
                @CreatedAt
            );
            """;

        return WithConnectionAsync(
            (connection, transaction) => connection.ExecuteAsync(
                new CommandDefinition(sql, auditLog, transaction, cancellationToken: cancellationToken)),
            cancellationToken);
    }
}

using Dapper;
using RMS.Identity.Service.Domain.Entities;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Infrastructure.Persistence.Internal;

namespace RMS.Identity.Service.Infrastructure.Persistence;

internal sealed class OutboxRepository : RepositoryBase, IOutboxRepository
{
    public OutboxRepository(MySqlConnectionFactory connectionFactory, DbSessionAccessor sessionAccessor)
        : base(connectionFactory, sessionAccessor)
    {
    }

    public Task CreateAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO Outbox
            (
                EventType,
                AggregateType,
                AggregateUUID,
                Payload,
                Status,
                Retries,
                CreatedAt,
                AvailableAt
            )
            VALUES
            (
                @EventType,
                @AggregateType,
                CASE
                    WHEN @AggregateUUID IS NULL THEN NULL
                    ELSE UUID_TO_BIN(@AggregateUUID)
                END,
                @PayloadJson,
                @Status,
                @Retries,
                @CreatedAt,
                @AvailableAt
            );
            """;

        return WithConnectionAsync(
            (connection, transaction) => connection.ExecuteAsync(
                new CommandDefinition(sql, message, transaction, cancellationToken: cancellationToken)),
            cancellationToken);
    }
}

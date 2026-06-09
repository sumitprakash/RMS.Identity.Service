using System.Text.Json;
using MySqlConnector;
using RMS.Identity.Service.Domain.Entities.Outbox;
using RMS.Identity.Service.Domain.Entities.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Outbox;
using RMS.Identity.Service.Infrastructure.Data;
using RMS.Identity.Service.Infrastructure.Persistence.Schema;

namespace RMS.Identity.Service.Infrastructure.Persistence.Outbox;

public sealed class OutboxMySqlRepository : IOutboxWriteRepository, IOutboxProcessingRepository
{
    private readonly IDatabaseTransactionAccessor _transactionAccessor;
    private readonly IMySqlConnectionFactory _connectionFactory;

    public OutboxMySqlRepository(
        IDatabaseTransactionAccessor transactionAccessor,
        IMySqlConnectionFactory connectionFactory)
    {
        _transactionAccessor = transactionAccessor;
        _connectionFactory = connectionFactory;
    }

    public async Task InsertEmailVerificationRequestedAsync(
        UserAccount account,
        string token,
        DateTime expiresAt,
        CancellationToken cancellationToken)
    {
        var databaseTransaction = _transactionAccessor.GetCurrent().AsMySql();
        var command = databaseTransaction.Connection.CreateCommand();
        command.Transaction = databaseTransaction.Transaction;
        command.CommandText =
            $"""
            INSERT INTO {OutboxTable.Name} (
                {OutboxTable.Columns.EventType},
                {OutboxTable.Columns.AggregateType},
                {OutboxTable.Columns.AggregateUuid},
                {OutboxTable.Columns.Payload},
                {OutboxTable.Columns.Status},
                {OutboxTable.Columns.Retries},
                {OutboxTable.Columns.CreatedAt},
                {OutboxTable.Columns.AvailableAt})
            VALUES (
                @EventType,
                @AggregateType,
                UUID_TO_BIN(@AggregateUuid),
                CAST(@Payload AS JSON),
                'pending',
                0,
                UTC_TIMESTAMP(),
                UTC_TIMESTAMP());
            """;
        command.Parameters.AddWithValue("@EventType", "email_verification_requested");
        command.Parameters.AddWithValue("@AggregateType", UserAccountTable.Name);
        command.Parameters.AddWithValue("@AggregateUuid", account.UserUuid.ToString());
        command.Parameters.AddWithValue(
            "@Payload",
            JsonSerializer.Serialize(new
            {
                account.UserUuid,
                EmailAddress = account.Username,
                Token = token,
                ExpiresAt = expiresAt
            }));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<OutboxMessage>> ClaimAvailableAsync(
        string eventType,
        int batchSize,
        int maxRetries,
        int processingTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var messages = await SelectAvailableAsync(
                connection,
                transaction,
                eventType,
                batchSize,
                maxRetries,
                cancellationToken);

            if (messages.Count > 0)
            {
                await MarkProcessingAsync(
                    connection,
                    transaction,
                    messages.Select(message => message.OutboxId).ToArray(),
                    processingTimeoutSeconds,
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return messages;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task MarkPublishedAsync(
        long outboxId,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
            UPDATE {OutboxTable.Name}
            SET {OutboxTable.Columns.Status} = 'published'
            WHERE {OutboxTable.Columns.OutboxId} = @OutboxId;
            """;
        command.Parameters.AddWithValue("@OutboxId", outboxId);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(
        long outboxId,
        DateTime availableAt,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
            UPDATE {OutboxTable.Name}
            SET {OutboxTable.Columns.Status} = 'failed',
                {OutboxTable.Columns.Retries} = {OutboxTable.Columns.Retries} + 1,
                {OutboxTable.Columns.AvailableAt} = @AvailableAt
            WHERE {OutboxTable.Columns.OutboxId} = @OutboxId;
            """;
        command.Parameters.AddWithValue("@OutboxId", outboxId);
        command.Parameters.AddWithValue("@AvailableAt", availableAt);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<List<OutboxMessage>> SelectAvailableAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        string eventType,
        int batchSize,
        int maxRetries,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            $"""
            SELECT
                {OutboxTable.Columns.OutboxId},
                {OutboxTable.Columns.EventType},
                CAST({OutboxTable.Columns.Payload} AS CHAR) AS {OutboxTable.Columns.Payload},
                {OutboxTable.Columns.Retries}
            FROM {OutboxTable.Name}
            WHERE {OutboxTable.Columns.EventType} = @EventType
              AND {OutboxTable.Columns.Status} IN ('pending', 'failed', 'processing')
              AND {OutboxTable.Columns.AvailableAt} <= UTC_TIMESTAMP()
              AND {OutboxTable.Columns.Retries} < @MaxRetries
            ORDER BY {OutboxTable.Columns.OutboxId}
            LIMIT @BatchSize
            FOR UPDATE SKIP LOCKED;
            """;
        command.Parameters.AddWithValue("@EventType", eventType);
        command.Parameters.AddWithValue("@BatchSize", batchSize);
        command.Parameters.AddWithValue("@MaxRetries", maxRetries);

        var messages = new List<OutboxMessage>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            messages.Add(new OutboxMessage(
                reader.GetInt64(reader.GetOrdinal(OutboxTable.Columns.OutboxId)),
                reader.GetString(OutboxTable.Columns.EventType),
                reader.GetString(OutboxTable.Columns.Payload),
                reader.GetInt32(reader.GetOrdinal(OutboxTable.Columns.Retries))));
        }

        return messages;
    }

    private static async Task MarkProcessingAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        IReadOnlyList<long> outboxIds,
        int processingTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;

        var parameterNames = new List<string>();
        for (var index = 0; index < outboxIds.Count; index++)
        {
            var parameterName = $"@OutboxId{index}";
            parameterNames.Add(parameterName);
            command.Parameters.AddWithValue(parameterName, outboxIds[index]);
        }

        command.CommandText =
            $"""
            UPDATE {OutboxTable.Name}
            SET {OutboxTable.Columns.Status} = 'processing',
                {OutboxTable.Columns.AvailableAt} = DATE_ADD(UTC_TIMESTAMP(), INTERVAL @ProcessingTimeoutSeconds SECOND)
            WHERE {OutboxTable.Columns.OutboxId} IN ({string.Join(", ", parameterNames)});
            """;
        command.Parameters.AddWithValue("@ProcessingTimeoutSeconds", processingTimeoutSeconds);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

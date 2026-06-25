using RMS.Identity.Service.Infrastructure.Data;
using RMS.Identity.Service.Infrastructure.Persistence.Schema;

namespace RMS.Identity.Service.Infrastructure.Maintenance;

public sealed class DataRetentionRepository
{
    private readonly IMySqlConnectionFactory _connectionFactory;

    public DataRetentionRepository(IMySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> PurgeAsync(
        DataRetentionOptions options,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var deleted = 0;
        deleted += await DeleteAsync(
            connection,
            $"""
            DELETE FROM {RefreshTokenTable.Name}
            WHERE ({RefreshTokenTable.Columns.ExpiresAt} < @Cutoff
                OR {RefreshTokenTable.Columns.RevokedAt} < @Cutoff)
            LIMIT @BatchSize;
            """,
            now.AddDays(-options.RefreshTokenRetentionDays),
            options.BatchSize,
            cancellationToken);
        deleted += await DeleteAsync(
            connection,
            $"""
            DELETE FROM {EmailVerificationTable.Name}
            WHERE {EmailVerificationTable.Columns.CreatedAt} < @Cutoff
              AND ({EmailVerificationTable.Columns.Consumed} = 1
                OR {EmailVerificationTable.Columns.ExpiresAt} < UTC_TIMESTAMP())
            LIMIT @BatchSize;
            """,
            now.AddDays(-options.VerificationTokenRetentionDays),
            options.BatchSize,
            cancellationToken);
        deleted += await DeleteAsync(
            connection,
            $"""
            DELETE FROM {IdempotencyKeyTable.Name}
            WHERE {IdempotencyKeyTable.Columns.CreatedAt} < @Cutoff
            LIMIT @BatchSize;
            """,
            now.AddDays(-options.IdempotencyRetentionDays),
            options.BatchSize,
            cancellationToken);
        deleted += await DeleteAsync(
            connection,
            $"""
            DELETE FROM {OutboxTable.Name}
            WHERE {OutboxTable.Columns.CreatedAt} < @Cutoff
              AND {OutboxTable.Columns.Status} IN ('published', 'failed')
            LIMIT @BatchSize;
            """,
            now.AddDays(-options.OutboxRetentionDays),
            options.BatchSize,
            cancellationToken);

        return deleted;
    }

    private static async Task<int> DeleteAsync(
        MySqlConnector.MySqlConnection connection,
        string commandText,
        DateTime cutoff,
        int batchSize,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.Parameters.AddWithValue("@Cutoff", cutoff);
        command.Parameters.AddWithValue("@BatchSize", batchSize);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

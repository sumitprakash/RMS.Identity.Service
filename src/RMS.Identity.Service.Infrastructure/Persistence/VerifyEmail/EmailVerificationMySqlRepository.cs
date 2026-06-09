using RMS.Identity.Service.Domain.Contracts.VerifyEmail;
using RMS.Identity.Service.Domain.Entities.VerifyEmail;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Domain.Interfaces.Repositories.VerifyEmail;
using RMS.Identity.Service.Infrastructure.Data;
using RMS.Identity.Service.Infrastructure.Persistence.Schema;

namespace RMS.Identity.Service.Infrastructure.Persistence.VerifyEmail;

public sealed class EmailVerificationMySqlRepository :
    IEmailVerificationReadRepository,
    IEmailVerificationWriteRepository
{
    private readonly IDatabaseTransactionAccessor _transactionAccessor;

    public EmailVerificationMySqlRepository(IDatabaseTransactionAccessor transactionAccessor)
    {
        _transactionAccessor = transactionAccessor;
    }

    public async Task CreateAsync(
        CreateEmailVerificationCommand command,
        CancellationToken cancellationToken)
    {
        var databaseTransaction = CurrentTransaction();
        var insertCommand = databaseTransaction.Connection.CreateCommand();
        insertCommand.Transaction = databaseTransaction.Transaction;
        insertCommand.CommandText =
            $"""
            INSERT INTO {EmailVerificationTable.Name} (
                {EmailVerificationTable.Columns.UserId},
                {EmailVerificationTable.Columns.TokenHash},
                {EmailVerificationTable.Columns.Purpose},
                {EmailVerificationTable.Columns.ExpiresAt},
                {EmailVerificationTable.Columns.CreatedAt},
                {EmailVerificationTable.Columns.Consumed})
            VALUES (
                @UserId,
                @TokenHash,
                @Purpose,
                @ExpiresAt,
                UTC_TIMESTAMP(),
                0);
            """;
        insertCommand.Parameters.AddWithValue("@UserId", command.UserId);
        insertCommand.Parameters.AddWithValue("@TokenHash", command.TokenHash);
        insertCommand.Parameters.AddWithValue("@Purpose", command.Purpose);
        insertCommand.Parameters.AddWithValue("@ExpiresAt", command.ExpiresAt);

        await insertCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<EmailVerificationToken?> GetByTokenHashAsync(
        string tokenHash,
        string purpose,
        CancellationToken cancellationToken)
    {
        var databaseTransaction = CurrentTransaction();
        var command = databaseTransaction.Connection.CreateCommand();
        command.Transaction = databaseTransaction.Transaction;
        command.CommandText =
            $"""
            SELECT
                {EmailVerificationTable.Columns.EmailVerificationId},
                {EmailVerificationTable.Columns.UserId},
                {EmailVerificationTable.Columns.TokenHash},
                {EmailVerificationTable.Columns.Purpose},
                {EmailVerificationTable.Columns.ExpiresAt},
                {EmailVerificationTable.Columns.Consumed}
            FROM {EmailVerificationTable.Name}
            WHERE {EmailVerificationTable.Columns.TokenHash} = @TokenHash
              AND {EmailVerificationTable.Columns.Purpose} = @Purpose
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@TokenHash", tokenHash);
        command.Parameters.AddWithValue("@Purpose", purpose);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new EmailVerificationToken(
            reader.GetInt64(reader.GetOrdinal(EmailVerificationTable.Columns.EmailVerificationId)),
            reader.GetInt64(reader.GetOrdinal(EmailVerificationTable.Columns.UserId)),
            reader.GetString(EmailVerificationTable.Columns.TokenHash),
            reader.GetString(EmailVerificationTable.Columns.Purpose),
            reader.GetUtcDateTime(EmailVerificationTable.Columns.ExpiresAt),
            reader.GetBoolean(reader.GetOrdinal(EmailVerificationTable.Columns.Consumed)));
    }

    public async Task<bool> TryConsumeAsync(
        long emailVerificationId,
        CancellationToken cancellationToken)
    {
        var databaseTransaction = CurrentTransaction();
        var updateCommand = databaseTransaction.Connection.CreateCommand();
        updateCommand.Transaction = databaseTransaction.Transaction;
        updateCommand.CommandText =
            $"""
            UPDATE {EmailVerificationTable.Name}
            SET {EmailVerificationTable.Columns.Consumed} = 1
            WHERE {EmailVerificationTable.Columns.EmailVerificationId} = @EmailVerificationId
              AND {EmailVerificationTable.Columns.Consumed} = 0;
            """;
        updateCommand.Parameters.AddWithValue("@EmailVerificationId", emailVerificationId);

        return await updateCommand.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private MySqlDatabaseTransaction CurrentTransaction() =>
        _transactionAccessor.GetCurrent().AsMySql();
}

using System.Text.Json;
using RMS.Identity.Service.Domain.Entities.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Outbox;
using RMS.Identity.Service.Infrastructure.Data;
using RMS.Identity.Service.Infrastructure.Persistence.Schema;

namespace RMS.Identity.Service.Infrastructure.Persistence.Outbox;

public sealed class OutboxMySqlRepository : IOutboxWriteRepository
{
    private readonly IDatabaseTransactionAccessor _transactionAccessor;

    public OutboxMySqlRepository(IDatabaseTransactionAccessor transactionAccessor)
    {
        _transactionAccessor = transactionAccessor;
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
}

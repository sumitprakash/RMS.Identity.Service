using System.Text.Json;
using RMS.Identity.Service.Domain.Entities.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Domain.Interfaces.Repositories.AuditLog;
using RMS.Identity.Service.Infrastructure.Data;
using RMS.Identity.Service.Infrastructure.Persistence.Schema;

namespace RMS.Identity.Service.Infrastructure.Persistence.AuditLog;

public sealed class AuditLogMySqlRepository : IAuditLogWriteRepository
{
    private readonly IDatabaseTransactionAccessor _transactionAccessor;

    public AuditLogMySqlRepository(IDatabaseTransactionAccessor transactionAccessor)
    {
        _transactionAccessor = transactionAccessor;
    }

    public async Task InsertSignUpCreatedAsync(
        UserAccount account,
        CancellationToken cancellationToken)
    {
        var databaseTransaction = _transactionAccessor.GetCurrent().AsMySql();
        var command = databaseTransaction.Connection.CreateCommand();
        command.Transaction = databaseTransaction.Transaction;
        command.CommandText =
            $"""
            INSERT INTO {AuditLogTable.Name} (
                {AuditLogTable.Columns.TableName},
                {AuditLogTable.Columns.RecordId},
                {AuditLogTable.Columns.Action},
                {AuditLogTable.Columns.Payload},
                {AuditLogTable.Columns.CreatedAt})
            VALUES (@TableName, @RecordId, @Action, CAST(@Payload AS JSON), UTC_TIMESTAMP());
            """;
        command.Parameters.AddWithValue("@TableName", UserAccountTable.Name);
        command.Parameters.AddWithValue("@RecordId", account.UserUuid.ToString());
        command.Parameters.AddWithValue("@Action", "signup_created");
        command.Parameters.AddWithValue(
            "@Payload",
            JsonSerializer.Serialize(new
            {
                account.UserUuid,
                account.Username,
                account.DisplayName,
                Status = "pending",
                account.CreatedAt
            }));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

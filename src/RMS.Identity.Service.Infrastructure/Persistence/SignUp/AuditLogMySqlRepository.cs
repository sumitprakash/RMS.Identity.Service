using System.Text.Json;
using RMS.Identity.Service.Domain.Entities.SignUp;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Domain.Interfaces.SignUp;
using RMS.Identity.Service.Infrastructure.Data;

namespace RMS.Identity.Service.Infrastructure.Persistence.SignUp;

public sealed class AuditLogMySqlRepository : IAuditLogRepository
{
    public async Task InsertSignUpCreatedAsync(
        IDatabaseTransaction transaction,
        SignUpUser createdUser,
        CancellationToken cancellationToken)
    {
        var databaseTransaction = transaction.AsMySql();
        var command = databaseTransaction.Connection.CreateCommand();
        command.Transaction = databaseTransaction.Transaction;
        command.CommandText =
            """
            INSERT INTO AuditLog (TableName, RecordId, Action, Payload, CreatedAt)
            VALUES ('UserAccount', @RecordId, 'signup_created', CAST(@Payload AS JSON), UTC_TIMESTAMP());
            """;
        command.Parameters.AddWithValue("@RecordId", createdUser.UserUuid.ToString());
        command.Parameters.AddWithValue("@Payload", JsonSerializer.Serialize(createdUser));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

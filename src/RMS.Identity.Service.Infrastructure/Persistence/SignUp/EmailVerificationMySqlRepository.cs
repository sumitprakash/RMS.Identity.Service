using RMS.Identity.Service.Domain.Contracts.SignUp;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Domain.Interfaces.SignUp;
using RMS.Identity.Service.Infrastructure.Data;

namespace RMS.Identity.Service.Infrastructure.Persistence.SignUp;

public sealed class EmailVerificationMySqlRepository : IEmailVerificationRepository
{
    public async Task CreateAsync(
        IDatabaseTransaction transaction,
        CreateEmailVerificationCommand command,
        CancellationToken cancellationToken)
    {
        var databaseTransaction = transaction.AsMySql();
        var insertCommand = databaseTransaction.Connection.CreateCommand();
        insertCommand.Transaction = databaseTransaction.Transaction;
        insertCommand.CommandText =
            """
            INSERT INTO EmailVerification (
                UserID,
                TokenHash,
                Purpose,
                ExpiresAt,
                CreatedAt,
                Consumed)
            VALUES (
                @UserId,
                @TokenHash,
                'email_verification',
                @ExpiresAt,
                UTC_TIMESTAMP(),
                0);
            """;
        insertCommand.Parameters.AddWithValue("@UserId", command.UserId);
        insertCommand.Parameters.AddWithValue("@TokenHash", command.TokenHash);
        insertCommand.Parameters.AddWithValue("@ExpiresAt", command.ExpiresAt);
        await insertCommand.ExecuteNonQueryAsync(cancellationToken);
    }
}

using Dapper;
using RMS.Identity.Service.Domain.Entities;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Infrastructure.Persistence.Internal;

namespace RMS.Identity.Service.Infrastructure.Persistence;

internal sealed class EmailVerificationRepository : RepositoryBase, IEmailVerificationRepository
{
    public EmailVerificationRepository(MySqlConnectionFactory connectionFactory, DbSessionAccessor sessionAccessor)
        : base(connectionFactory, sessionAccessor)
    {
    }

    public async Task CreateAsync(EmailVerification verification, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO EmailVerification
            (
                UserID,
                TokenHash,
                Purpose,
                ExpiresAt,
                CreatedAt,
                Consumed
            )
            VALUES
            (
                @UserID,
                @TokenHash,
                @Purpose,
                @ExpiresAt,
                @CreatedAt,
                @Consumed
            );
            SELECT LAST_INSERT_ID();
            """;

        verification.EmailVerificationID = await WithConnectionAsync(
            (connection, transaction) => connection.ExecuteScalarAsync<long>(
                new CommandDefinition(sql, verification, transaction, cancellationToken: cancellationToken)),
            cancellationToken);
    }

    public Task<EmailVerification?> GetActiveByTokenHashAsync(string tokenHash, string purpose, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                EmailVerificationID,
                UserID,
                TokenHash,
                Purpose,
                ExpiresAt,
                CreatedAt,
                Consumed
            FROM EmailVerification
            WHERE TokenHash = @TokenHash
              AND Purpose = @Purpose
              AND Consumed = 0
            LIMIT 1;
            """;

        return WithConnectionAsync(
            (connection, transaction) => connection.QuerySingleOrDefaultAsync<EmailVerification>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        TokenHash = tokenHash,
                        Purpose = purpose
                    },
                    transaction,
                    cancellationToken: cancellationToken)),
            cancellationToken);
    }

    public Task MarkConsumedAsync(long emailVerificationId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE EmailVerification
            SET Consumed = 1
            WHERE EmailVerificationID = @EmailVerificationID;
            """;

        return WithConnectionAsync(
            (connection, transaction) => connection.ExecuteAsync(
                new CommandDefinition(sql, new { EmailVerificationID = emailVerificationId }, transaction, cancellationToken: cancellationToken)),
            cancellationToken);
    }
}

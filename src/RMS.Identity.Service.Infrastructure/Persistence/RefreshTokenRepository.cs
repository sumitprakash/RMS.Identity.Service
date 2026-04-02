using Dapper;
using RMS.Identity.Service.Domain.Entities;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Infrastructure.Persistence.Internal;

namespace RMS.Identity.Service.Infrastructure.Persistence;

internal sealed class RefreshTokenRepository : RepositoryBase, IRefreshTokenRepository
{
    public RefreshTokenRepository(MySqlConnectionFactory connectionFactory, DbSessionAccessor sessionAccessor)
        : base(connectionFactory, sessionAccessor)
    {
    }

    public async Task CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO RefreshToken
            (
                UserID,
                TokenHash,
                ExpiresAt,
                CreatedAt,
                RevokedAt,
                ReplacedByTokenHash
            )
            VALUES
            (
                @UserID,
                @TokenHash,
                @ExpiresAt,
                @CreatedAt,
                @RevokedAt,
                @ReplacedByTokenHash
            );
            SELECT LAST_INSERT_ID();
            """;

        refreshToken.RefreshTokenID = await WithConnectionAsync(
            (connection, transaction) => connection.ExecuteScalarAsync<long>(
                new CommandDefinition(sql, refreshToken, transaction, cancellationToken: cancellationToken)),
            cancellationToken);
    }

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                RefreshTokenID,
                UserID,
                TokenHash,
                ExpiresAt,
                CreatedAt,
                RevokedAt,
                ReplacedByTokenHash
            FROM RefreshToken
            WHERE TokenHash = @TokenHash
            LIMIT 1;
            """;

        return WithConnectionAsync(
            (connection, transaction) => connection.QuerySingleOrDefaultAsync<RefreshToken>(
                new CommandDefinition(sql, new { TokenHash = tokenHash }, transaction, cancellationToken: cancellationToken)),
            cancellationToken);
    }

    public Task RevokeAsync(long refreshTokenId, string? replacedByTokenHash, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE RefreshToken
            SET RevokedAt = UTC_TIMESTAMP(),
                ReplacedByTokenHash = @ReplacedByTokenHash
            WHERE RefreshTokenID = @RefreshTokenID;
            """;

        return WithConnectionAsync(
            (connection, transaction) => connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        RefreshTokenID = refreshTokenId,
                        ReplacedByTokenHash = replacedByTokenHash
                    },
                    transaction,
                    cancellationToken: cancellationToken)),
            cancellationToken);
    }
}

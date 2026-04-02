using Dapper;
using RMS.Identity.Service.Domain.Entities;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Infrastructure.Persistence.Internal;

namespace RMS.Identity.Service.Infrastructure.Persistence;

internal sealed class UserAccountRepository : RepositoryBase, IUserAccountRepository
{
    public UserAccountRepository(MySqlConnectionFactory connectionFactory, DbSessionAccessor sessionAccessor)
        : base(connectionFactory, sessionAccessor)
    {
    }

    public Task<UserAccount?> GetByIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                UserID,
                BIN_TO_UUID(UserUUID) AS UserUUID,
                CompanyID,
                Username,
                PasswordHash,
                DisplayName,
                EmailVerified,
                IsActive,
                IsDeleted,
                LastLoginAt,
                FailedLoginCount,
                LockedUntil,
                CreatedAt,
                CreatedBy,
                UpdatedAt,
                UpdatedBy
            FROM UserAccount
            WHERE UserID = @UserID
              AND IsDeleted = 0
            LIMIT 1;
            """;

        return WithConnectionAsync(
            (connection, transaction) => connection.QuerySingleOrDefaultAsync<UserAccount>(
                new CommandDefinition(sql, new { UserID = userId }, transaction, cancellationToken: cancellationToken)),
            cancellationToken);
    }

    public Task<UserAccount?> GetByUuidAsync(Guid userUuid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                UserID,
                BIN_TO_UUID(UserUUID) AS UserUUID,
                CompanyID,
                Username,
                PasswordHash,
                DisplayName,
                EmailVerified,
                IsActive,
                IsDeleted,
                LastLoginAt,
                FailedLoginCount,
                LockedUntil,
                CreatedAt,
                CreatedBy,
                UpdatedAt,
                UpdatedBy
            FROM UserAccount
            WHERE UserUUID = UUID_TO_BIN(@UserUUID)
              AND IsDeleted = 0
            LIMIT 1;
            """;

        return WithConnectionAsync(
            (connection, transaction) => connection.QuerySingleOrDefaultAsync<UserAccount>(
                new CommandDefinition(sql, new { UserUUID = userUuid }, transaction, cancellationToken: cancellationToken)),
            cancellationToken);
    }

    public Task<UserAccount?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                UserID,
                BIN_TO_UUID(UserUUID) AS UserUUID,
                CompanyID,
                Username,
                PasswordHash,
                DisplayName,
                EmailVerified,
                IsActive,
                IsDeleted,
                LastLoginAt,
                FailedLoginCount,
                LockedUntil,
                CreatedAt,
                CreatedBy,
                UpdatedAt,
                UpdatedBy
            FROM UserAccount
            WHERE Username = @Username
              AND IsDeleted = 0
            LIMIT 1;
            """;

        return WithConnectionAsync(
            (connection, transaction) => connection.QuerySingleOrDefaultAsync<UserAccount>(
                new CommandDefinition(sql, new { Username = username }, transaction, cancellationToken: cancellationToken)),
            cancellationToken);
    }

    public Task<long> CreateAsync(UserAccount user, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO UserAccount
            (
                UserUUID,
                CompanyID,
                Username,
                PasswordHash,
                DisplayName,
                EmailVerified,
                IsActive,
                IsDeleted,
                LastLoginAt,
                FailedLoginCount,
                LockedUntil,
                CreatedAt,
                CreatedBy,
                UpdatedAt,
                UpdatedBy
            )
            VALUES
            (
                UUID_TO_BIN(@UserUUID),
                @CompanyID,
                @Username,
                @PasswordHash,
                @DisplayName,
                @EmailVerified,
                @IsActive,
                @IsDeleted,
                @LastLoginAt,
                @FailedLoginCount,
                @LockedUntil,
                @CreatedAt,
                @CreatedBy,
                @UpdatedAt,
                @UpdatedBy
            );
            SELECT LAST_INSERT_ID();
            """;

        return WithConnectionAsync(
            (connection, transaction) => connection.ExecuteScalarAsync<long>(
                new CommandDefinition(sql, user, transaction, cancellationToken: cancellationToken)),
            cancellationToken);
    }

    public Task MarkEmailVerifiedAsync(long userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE UserAccount
            SET EmailVerified = 1,
                UpdatedAt = UTC_TIMESTAMP()
            WHERE UserID = @UserID;
            """;

        return WithConnectionAsync(
            (connection, transaction) => connection.ExecuteAsync(
                new CommandDefinition(sql, new { UserID = userId }, transaction, cancellationToken: cancellationToken)),
            cancellationToken);
    }

    public Task RecordSuccessfulLoginAsync(long userId, DateTime loginAtUtc, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE UserAccount
            SET LastLoginAt = @LoginAtUtc,
                FailedLoginCount = 0,
                UpdatedAt = @LoginAtUtc
            WHERE UserID = @UserID;
            """;

        return WithConnectionAsync(
            (connection, transaction) => connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new { UserID = userId, LoginAtUtc = loginAtUtc },
                    transaction,
                    cancellationToken: cancellationToken)),
            cancellationToken);
    }

    public Task RecordFailedLoginAsync(long userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE UserAccount
            SET FailedLoginCount = FailedLoginCount + 1,
                UpdatedAt = UTC_TIMESTAMP()
            WHERE UserID = @UserID;
            """;

        return WithConnectionAsync(
            (connection, transaction) => connection.ExecuteAsync(
                new CommandDefinition(sql, new { UserID = userId }, transaction, cancellationToken: cancellationToken)),
            cancellationToken);
    }
}

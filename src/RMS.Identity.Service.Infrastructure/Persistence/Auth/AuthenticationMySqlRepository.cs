using MySqlConnector;
using RMS.Identity.Service.Domain.Entities.Auth;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Auth;
using RMS.Identity.Service.Infrastructure.Data;
using RMS.Identity.Service.Infrastructure.Persistence.Schema;

namespace RMS.Identity.Service.Infrastructure.Persistence.Auth;

public sealed class AuthenticationMySqlRepository : IAuthenticationRepository
{
    private const int MaxFailedLoginAttempts = 5;
    private const int LockoutMinutes = 15;

    private readonly IMySqlConnectionFactory _connectionFactory;

    public AuthenticationMySqlRepository(IMySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AuthenticatedUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
            SELECT
                ua.{UserAccountTable.Columns.UserId},
                BIN_TO_UUID(ua.{UserAccountTable.Columns.UserUuid}) AS UserUuid,
                BIN_TO_UUID(c.{CompanyTable.Columns.CompanyUuid}) AS CompanyUuid,
                ua.{UserAccountTable.Columns.Username},
                ua.{UserAccountTable.Columns.PasswordHash},
                ua.{UserAccountTable.Columns.DisplayName},
                ua.{UserAccountTable.Columns.PasswordSetupRequired},
                ua.{UserAccountTable.Columns.EmailVerified},
                ua.{UserAccountTable.Columns.IsActive},
                ua.{UserAccountTable.Columns.IsDeleted},
                ua.{UserAccountTable.Columns.LockedUntil},
                ua.{UserAccountTable.Columns.CreatedAt}
            FROM {UserAccountTable.Name} ua
            LEFT JOIN {CompanyTable.Name} c
                ON c.{CompanyTable.Columns.CompanyId} = ua.{UserAccountTable.Columns.CompanyId}
                AND c.{CompanyTable.Columns.IsDeleted} = 0
            WHERE ua.{UserAccountTable.Columns.Username} = @Username
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@Username", username);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var userId = reader.GetInt64(reader.GetOrdinal(UserAccountTable.Columns.UserId));
        var user = new AuthenticatedUser(
            userId,
            Guid.Parse(reader.GetString("UserUuid")),
            reader.IsDBNull(reader.GetOrdinal("CompanyUuid"))
                ? null
                : Guid.Parse(reader.GetString("CompanyUuid")),
            reader.GetString(UserAccountTable.Columns.Username),
            reader.GetString(UserAccountTable.Columns.PasswordHash),
            reader.GetNullableString(UserAccountTable.Columns.DisplayName),
            reader.GetBoolean(reader.GetOrdinal(UserAccountTable.Columns.EmailVerified)),
            reader.GetBoolean(reader.GetOrdinal(UserAccountTable.Columns.IsActive)),
            reader.GetBoolean(reader.GetOrdinal(UserAccountTable.Columns.IsDeleted)),
            reader.IsDBNull(reader.GetOrdinal(UserAccountTable.Columns.LockedUntil))
                ? null
                : DateTime.SpecifyKind(reader.GetDateTime(reader.GetOrdinal(UserAccountTable.Columns.LockedUntil)), DateTimeKind.Utc),
            reader.GetUtcDateTime(UserAccountTable.Columns.CreatedAt),
            Array.Empty<string>())
        {
            PasswordSetupRequired = reader.GetBoolean(reader.GetOrdinal(UserAccountTable.Columns.PasswordSetupRequired))
        };

        await reader.CloseAsync();
        return user with
        {
            Roles = await GetRolesAsync(connection, userId, cancellationToken)
        };
    }

    public async Task<RefreshTokenSession?> GetRefreshTokenSessionAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
            SELECT
                rt.{RefreshTokenTable.Columns.RefreshTokenId},
                rt.{RefreshTokenTable.Columns.TokenHash},
                rt.{RefreshTokenTable.Columns.ExpiresAt},
                rt.{RefreshTokenTable.Columns.RevokedAt},
                ua.{UserAccountTable.Columns.UserId},
                BIN_TO_UUID(ua.{UserAccountTable.Columns.UserUuid}) AS UserUuid,
                BIN_TO_UUID(c.{CompanyTable.Columns.CompanyUuid}) AS CompanyUuid,
                ua.{UserAccountTable.Columns.Username},
                ua.{UserAccountTable.Columns.PasswordHash},
                ua.{UserAccountTable.Columns.DisplayName},
                ua.{UserAccountTable.Columns.PasswordSetupRequired},
                ua.{UserAccountTable.Columns.EmailVerified},
                ua.{UserAccountTable.Columns.IsActive},
                ua.{UserAccountTable.Columns.IsDeleted},
                ua.{UserAccountTable.Columns.LockedUntil},
                ua.{UserAccountTable.Columns.CreatedAt}
            FROM {RefreshTokenTable.Name} rt
            INNER JOIN {UserAccountTable.Name} ua
                ON ua.{UserAccountTable.Columns.UserId} = rt.{RefreshTokenTable.Columns.UserId}
            LEFT JOIN {CompanyTable.Name} c
                ON c.{CompanyTable.Columns.CompanyId} = ua.{UserAccountTable.Columns.CompanyId}
                AND c.{CompanyTable.Columns.IsDeleted} = 0
            WHERE rt.{RefreshTokenTable.Columns.TokenHash} = @TokenHash
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@TokenHash", refreshTokenHash);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var userId = reader.GetInt64(reader.GetOrdinal(UserAccountTable.Columns.UserId));
        var user = new AuthenticatedUser(
            userId,
            Guid.Parse(reader.GetString("UserUuid")),
            reader.IsDBNull(reader.GetOrdinal("CompanyUuid"))
                ? null
                : Guid.Parse(reader.GetString("CompanyUuid")),
            reader.GetString(UserAccountTable.Columns.Username),
            reader.GetString(UserAccountTable.Columns.PasswordHash),
            reader.GetNullableString(UserAccountTable.Columns.DisplayName),
            reader.GetBoolean(reader.GetOrdinal(UserAccountTable.Columns.EmailVerified)),
            reader.GetBoolean(reader.GetOrdinal(UserAccountTable.Columns.IsActive)),
            reader.GetBoolean(reader.GetOrdinal(UserAccountTable.Columns.IsDeleted)),
            reader.IsDBNull(reader.GetOrdinal(UserAccountTable.Columns.LockedUntil))
                ? null
                : DateTime.SpecifyKind(reader.GetDateTime(reader.GetOrdinal(UserAccountTable.Columns.LockedUntil)), DateTimeKind.Utc),
            reader.GetUtcDateTime(UserAccountTable.Columns.CreatedAt),
            Array.Empty<string>())
        {
            PasswordSetupRequired = reader.GetBoolean(reader.GetOrdinal(UserAccountTable.Columns.PasswordSetupRequired))
        };
        var session = new RefreshTokenSession(
            reader.GetInt64(reader.GetOrdinal(RefreshTokenTable.Columns.RefreshTokenId)),
            reader.GetString(RefreshTokenTable.Columns.TokenHash),
            reader.GetUtcDateTime(RefreshTokenTable.Columns.ExpiresAt),
            reader.IsDBNull(reader.GetOrdinal(RefreshTokenTable.Columns.RevokedAt))
                ? null
                : DateTime.SpecifyKind(reader.GetDateTime(reader.GetOrdinal(RefreshTokenTable.Columns.RevokedAt)), DateTimeKind.Utc),
            user);

        await reader.CloseAsync();
        return session with
        {
            User = user with
            {
                Roles = await GetRolesAsync(connection, userId, cancellationToken)
            }
        };
    }

    public async Task RecordFailedLoginAsync(long userId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var failedLoginCount = 0;
            DateTime? lockedUntil = null;
            var databaseUtcNow = default(DateTime);
            var userExists = false;

            await using (var selectCommand = connection.CreateCommand())
            {
                selectCommand.Transaction = transaction;
                selectCommand.CommandText =
                    $"""
                    SELECT
                        {UserAccountTable.Columns.FailedLoginCount},
                        {UserAccountTable.Columns.LockedUntil},
                        UTC_TIMESTAMP() AS DatabaseUtcNow
                    FROM {UserAccountTable.Name}
                    WHERE {UserAccountTable.Columns.UserId} = @UserId
                    FOR UPDATE;
                    """;
                selectCommand.Parameters.AddWithValue("@UserId", userId);

                await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    userExists = true;
                    failedLoginCount = reader.GetInt32(reader.GetOrdinal(UserAccountTable.Columns.FailedLoginCount));
                    lockedUntil = reader.IsDBNull(reader.GetOrdinal(UserAccountTable.Columns.LockedUntil))
                        ? null
                        : DateTime.SpecifyKind(reader.GetDateTime(reader.GetOrdinal(UserAccountTable.Columns.LockedUntil)), DateTimeKind.Utc);
                    databaseUtcNow = DateTime.SpecifyKind(reader.GetDateTime(reader.GetOrdinal("DatabaseUtcNow")), DateTimeKind.Utc);
                }
            }

            if (!userExists)
            {
                await transaction.CommitAsync(cancellationToken);
                return;
            }

            var lockExpired = lockedUntil is not null && lockedUntil <= databaseUtcNow;
            var nextFailedLoginCount = lockExpired ? 1 : failedLoginCount + 1;
            var shouldLock = !lockExpired && nextFailedLoginCount >= MaxFailedLoginAttempts;
            var nextLockedUntil = lockExpired ? null : lockedUntil;

            await using var updateCommand = connection.CreateCommand();
            updateCommand.Transaction = transaction;
            updateCommand.CommandText =
                $"""
                UPDATE {UserAccountTable.Name}
                SET {UserAccountTable.Columns.FailedLoginCount} = @FailedLoginCount,
                    {UserAccountTable.Columns.LockedUntil} = CASE
                        WHEN @ShouldLock = 1 THEN DATE_ADD(UTC_TIMESTAMP(), INTERVAL {LockoutMinutes} MINUTE)
                        ELSE @LockedUntil
                    END,
                    {UserAccountTable.Columns.UpdatedAt} = UTC_TIMESTAMP()
                WHERE {UserAccountTable.Columns.UserId} = @UserId;
                """;
            updateCommand.Parameters.AddWithValue("@FailedLoginCount", nextFailedLoginCount);
            updateCommand.Parameters.AddWithValue("@ShouldLock", shouldLock ? 1 : 0);
            updateCommand.Parameters.AddWithValue("@LockedUntil", (object?)nextLockedUntil ?? DBNull.Value);
            updateCommand.Parameters.AddWithValue("@UserId", userId);
            await updateCommand.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task RecordSuccessfulLoginAsync(
        long userId,
        string refreshTokenHash,
        DateTime refreshTokenExpiresAt,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using (var updateUserCommand = connection.CreateCommand())
            {
                updateUserCommand.Transaction = transaction;
                updateUserCommand.CommandText =
                    $"""
                    UPDATE {UserAccountTable.Name}
                    SET {UserAccountTable.Columns.LastLoginAt} = UTC_TIMESTAMP(),
                        {UserAccountTable.Columns.FailedLoginCount} = 0,
                        {UserAccountTable.Columns.LockedUntil} = NULL,
                        {UserAccountTable.Columns.UpdatedAt} = UTC_TIMESTAMP()
                    WHERE {UserAccountTable.Columns.UserId} = @UserId;
                    """;
                updateUserCommand.Parameters.AddWithValue("@UserId", userId);
                await updateUserCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var insertTokenCommand = connection.CreateCommand())
            {
                insertTokenCommand.Transaction = transaction;
                insertTokenCommand.CommandText =
                    $"""
                    INSERT INTO {RefreshTokenTable.Name} (
                        {RefreshTokenTable.Columns.UserId},
                        {RefreshTokenTable.Columns.TokenHash},
                        {RefreshTokenTable.Columns.ExpiresAt})
                    VALUES (
                        @UserId,
                        @TokenHash,
                        @ExpiresAt);
                    """;
                insertTokenCommand.Parameters.AddWithValue("@UserId", userId);
                insertTokenCommand.Parameters.AddWithValue("@TokenHash", refreshTokenHash);
                insertTokenCommand.Parameters.AddWithValue("@ExpiresAt", refreshTokenExpiresAt);
                await insertTokenCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> RotateRefreshTokenAsync(
        long refreshTokenId,
        long userId,
        string newRefreshTokenHash,
        DateTime newRefreshTokenExpiresAt,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using (var revokeCommand = connection.CreateCommand())
            {
                revokeCommand.Transaction = transaction;
                revokeCommand.CommandText =
                    $"""
                    UPDATE {RefreshTokenTable.Name}
                    SET {RefreshTokenTable.Columns.RevokedAt} = UTC_TIMESTAMP(),
                        {RefreshTokenTable.Columns.ReplacedByTokenHash} = @NewTokenHash
                    WHERE {RefreshTokenTable.Columns.RefreshTokenId} = @RefreshTokenId
                      AND {RefreshTokenTable.Columns.UserId} = @UserId
                      AND {RefreshTokenTable.Columns.RevokedAt} IS NULL;
                    """;
                revokeCommand.Parameters.AddWithValue("@RefreshTokenId", refreshTokenId);
                revokeCommand.Parameters.AddWithValue("@UserId", userId);
                revokeCommand.Parameters.AddWithValue("@NewTokenHash", newRefreshTokenHash);

                var revokedRows = await revokeCommand.ExecuteNonQueryAsync(cancellationToken);
                if (revokedRows != 1)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return false;
                }
            }

            await using (var insertTokenCommand = connection.CreateCommand())
            {
                insertTokenCommand.Transaction = transaction;
                insertTokenCommand.CommandText =
                    $"""
                    INSERT INTO {RefreshTokenTable.Name} (
                        {RefreshTokenTable.Columns.UserId},
                        {RefreshTokenTable.Columns.TokenHash},
                        {RefreshTokenTable.Columns.ExpiresAt})
                    VALUES (
                        @UserId,
                        @TokenHash,
                        @ExpiresAt);
                    """;
                insertTokenCommand.Parameters.AddWithValue("@UserId", userId);
                insertTokenCommand.Parameters.AddWithValue("@TokenHash", newRefreshTokenHash);
                insertTokenCommand.Parameters.AddWithValue("@ExpiresAt", newRefreshTokenExpiresAt);
                await insertTokenCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task<IReadOnlyCollection<string>> GetRolesAsync(
        MySqlConnection connection,
        long userId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
            SELECT r.{RoleTable.Columns.Name}
            FROM {UserRoleTable.Name} ur
            INNER JOIN {RoleTable.Name} r
                ON r.{RoleTable.Columns.RoleId} = ur.{UserRoleTable.Columns.RoleId}
            WHERE ur.{UserRoleTable.Columns.UserId} = @UserId
              AND r.{RoleTable.Columns.IsDeleted} = 0
            ORDER BY r.{RoleTable.Columns.Name};
            """;
        command.Parameters.AddWithValue("@UserId", userId);

        var roles = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            roles.Add(reader.GetString(RoleTable.Columns.Name));
        }

        return roles;
    }
}

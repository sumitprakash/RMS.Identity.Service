using MySqlConnector;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.UserAccounts;
using RMS.Identity.Service.Domain.Entities.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Domain.Interfaces.UserAccounts;
using RMS.Identity.Service.Infrastructure.Data;
using System.Net;

namespace RMS.Identity.Service.Infrastructure.Persistence.UserAccounts;

public sealed class UserAccountMySqlRepository : IUserAccountRepository
{
    public async Task<bool> ExistsByUsernameAsync(
        IDatabaseTransaction transaction,
        string username,
        CancellationToken cancellationToken)
    {
        var databaseTransaction = transaction.AsMySql();
        var command = databaseTransaction.Connection.CreateCommand();
        command.Transaction = databaseTransaction.Transaction;
        command.CommandText =
            """
            SELECT 1
            FROM UserAccount
            WHERE Username = @Username
              AND IsDeleted = 0
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@Username", username);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    public async Task<long> CreateAsync(
        IDatabaseTransaction transaction,
        CreateUserAccountCommand command,
        CancellationToken cancellationToken)
    {
        var databaseTransaction = transaction.AsMySql();
        var insertCommand = databaseTransaction.Connection.CreateCommand();
        insertCommand.Transaction = databaseTransaction.Transaction;
        insertCommand.CommandText =
            """
            INSERT INTO UserAccount (
                UserUUID,
                CompanyID,
                Username,
                PasswordHash,
                DisplayName,
                EmailVerified,
                IsActive,
                IsDeleted,
                CreatedAt)
            VALUES (
                UUID_TO_BIN(@UserUuid),
                NULL,
                @Username,
                @PasswordHash,
                @DisplayName,
                0,
                1,
                0,
                UTC_TIMESTAMP());
            """;
        insertCommand.Parameters.AddWithValue("@UserUuid", command.UserUuid.ToString());
        insertCommand.Parameters.AddWithValue("@Username", command.Username);
        insertCommand.Parameters.AddWithValue("@PasswordHash", command.PasswordHash);
        insertCommand.Parameters.AddWithValue("@DisplayName", (object?)command.DisplayName ?? DBNull.Value);

        try
        {
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
            return insertCommand.LastInsertedId;
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            throw new ServiceException((int)HttpStatusCode.Conflict, "USER_EXISTS", "Username already exists.");
        }
    }

    public async Task<UserAccount> GetByIdAsync(
        IDatabaseTransaction transaction,
        long userId,
        CancellationToken cancellationToken)
    {
        var databaseTransaction = transaction.AsMySql();
        var command = databaseTransaction.Connection.CreateCommand();
        command.Transaction = databaseTransaction.Transaction;
        command.CommandText =
            """
            SELECT
                BIN_TO_UUID(UserUUID) AS UserUuid,
                Username,
                DisplayName,
                EmailVerified,
                IsActive,
                IsDeleted,
                CreatedAt
            FROM UserAccount
            WHERE UserID = @UserId
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@UserId", userId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new ServiceException(
                (int)HttpStatusCode.InternalServerError,
                "USER_ACCOUNT_READ_FAILED",
                "User account could not be loaded.");
        }

        return new UserAccount(
            userId,
            Guid.Parse(reader.GetString("UserUuid")),
            reader.GetString("Username"),
            reader.GetNullableString("DisplayName"),
            reader.GetBoolean(reader.GetOrdinal("EmailVerified")),
            reader.GetBoolean(reader.GetOrdinal("IsActive")),
            reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
            reader.GetUtcDateTime("CreatedAt"));
    }
}

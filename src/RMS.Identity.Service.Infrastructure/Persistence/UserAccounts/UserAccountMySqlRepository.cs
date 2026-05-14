using MySqlConnector;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.UserAccounts;
using RMS.Identity.Service.Domain.Entities.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Domain.Interfaces.Repositories.UserAccounts;
using RMS.Identity.Service.Infrastructure.Data;
using RMS.Identity.Service.Infrastructure.Persistence.Schema;
using System.Net;

namespace RMS.Identity.Service.Infrastructure.Persistence.UserAccounts;

public sealed class UserAccountMySqlRepository : IUserAccountRepository
{
    private readonly IDatabaseTransactionAccessor _transactionAccessor;

    public UserAccountMySqlRepository(IDatabaseTransactionAccessor transactionAccessor)
    {
        _transactionAccessor = transactionAccessor;
    }

    public async Task<bool> ExistsByUsernameAsync(
        string username,
        CancellationToken cancellationToken)
    {
        var databaseTransaction = CurrentTransaction();
        var command = databaseTransaction.Connection.CreateCommand();
        command.Transaction = databaseTransaction.Transaction;
        command.CommandText =
            $"""
            SELECT 1
            FROM {UserAccountTable.Name}
            WHERE {UserAccountTable.Columns.Username} = @Username
              AND {UserAccountTable.Columns.IsDeleted} = 0
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@Username", username);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    public async Task<long> CreateAsync(
        CreateUserAccountCommand command,
        CancellationToken cancellationToken)
    {
        var databaseTransaction = CurrentTransaction();
        var insertCommand = databaseTransaction.Connection.CreateCommand();
        insertCommand.Transaction = databaseTransaction.Transaction;
        insertCommand.CommandText =
            $"""
            INSERT INTO {UserAccountTable.Name} (
                {UserAccountTable.Columns.UserUuid},
                {UserAccountTable.Columns.CompanyId},
                {UserAccountTable.Columns.Username},
                {UserAccountTable.Columns.PasswordHash},
                {UserAccountTable.Columns.DisplayName},
                {UserAccountTable.Columns.EmailVerified},
                {UserAccountTable.Columns.IsActive},
                {UserAccountTable.Columns.IsDeleted},
                {UserAccountTable.Columns.CreatedAt})
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
            throw new ServiceException((int)HttpStatusCode.Conflict, "USER_EXISTS", "Email address already exists.");
        }
    }

    public async Task<UserAccount> GetByIdAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        var databaseTransaction = CurrentTransaction();
        var command = databaseTransaction.Connection.CreateCommand();
        command.Transaction = databaseTransaction.Transaction;
        command.CommandText =
            $"""
            SELECT
                BIN_TO_UUID({UserAccountTable.Columns.UserUuid}) AS UserUuid,
                {UserAccountTable.Columns.Username},
                {UserAccountTable.Columns.DisplayName},
                {UserAccountTable.Columns.EmailVerified},
                {UserAccountTable.Columns.IsActive},
                {UserAccountTable.Columns.IsDeleted},
                {UserAccountTable.Columns.CreatedAt}
            FROM {UserAccountTable.Name}
            WHERE {UserAccountTable.Columns.UserId} = @UserId
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
            reader.GetString(UserAccountTable.Columns.Username),
            reader.GetNullableString(UserAccountTable.Columns.DisplayName),
            reader.GetBoolean(reader.GetOrdinal(UserAccountTable.Columns.EmailVerified)),
            reader.GetBoolean(reader.GetOrdinal(UserAccountTable.Columns.IsActive)),
            reader.GetBoolean(reader.GetOrdinal(UserAccountTable.Columns.IsDeleted)),
            reader.GetUtcDateTime(UserAccountTable.Columns.CreatedAt));
    }

    private MySqlDatabaseTransaction CurrentTransaction() =>
        _transactionAccessor.GetCurrent().AsMySql();
}

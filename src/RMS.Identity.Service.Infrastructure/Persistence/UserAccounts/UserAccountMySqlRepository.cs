using MySqlConnector;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.UserAccounts;
using RMS.Identity.Service.Domain.Entities.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Domain.Interfaces.Repositories.UserAccounts;
using RMS.Identity.Service.Infrastructure.Data;
using RMS.Identity.Service.Infrastructure.Persistence.Schema;

namespace RMS.Identity.Service.Infrastructure.Persistence.UserAccounts;

public sealed class UserAccountMySqlRepository :
    IUserAccountReadRepository,
    IUserAccountWriteRepository
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
                {UserAccountTable.Columns.PhoneNumber},
                {UserAccountTable.Columns.PasswordSetupRequired},
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
                @PhoneNumber,
                @PasswordSetupRequired,
                0,
                1,
                0,
                UTC_TIMESTAMP());
            """;
        insertCommand.Parameters.AddWithValue("@UserUuid", command.UserUuid.ToString());
        insertCommand.Parameters.AddWithValue("@Username", command.Username);
        insertCommand.Parameters.AddWithValue("@PasswordHash", command.PasswordHash);
        insertCommand.Parameters.AddWithValue("@DisplayName", (object?)command.DisplayName ?? DBNull.Value);
        insertCommand.Parameters.AddWithValue("@PhoneNumber", (object?)command.PhoneNumber ?? DBNull.Value);
        insertCommand.Parameters.AddWithValue("@PasswordSetupRequired", command.PasswordSetupRequired);

        try
        {
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
            return insertCommand.LastInsertedId;
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            throw new ApplicationServiceException(ServiceErrorDefinitions.Users.UserExists);
        }
    }

    public async Task MarkEmailVerifiedAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        var databaseTransaction = CurrentTransaction();
        var updateCommand = databaseTransaction.Connection.CreateCommand();
        updateCommand.Transaction = databaseTransaction.Transaction;
        updateCommand.CommandText =
            $"""
            UPDATE {UserAccountTable.Name}
            SET {UserAccountTable.Columns.EmailVerified} = 1
            WHERE {UserAccountTable.Columns.UserId} = @UserId
              AND {UserAccountTable.Columns.IsDeleted} = 0;
            """;
        updateCommand.Parameters.AddWithValue("@UserId", userId);
        await updateCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task CompletePasswordSetupAsync(
        long userId,
        string passwordHash,
        CancellationToken cancellationToken)
    {
        var databaseTransaction = CurrentTransaction();
        var updateCommand = databaseTransaction.Connection.CreateCommand();
        updateCommand.Transaction = databaseTransaction.Transaction;
        updateCommand.CommandText =
            $"""
            UPDATE {UserAccountTable.Name}
            SET {UserAccountTable.Columns.PasswordHash} = @PasswordHash,
                {UserAccountTable.Columns.PasswordSetupRequired} = 0,
                {UserAccountTable.Columns.EmailVerified} = 1,
                {UserAccountTable.Columns.UpdatedAt} = UTC_TIMESTAMP()
            WHERE {UserAccountTable.Columns.UserId} = @UserId
              AND {UserAccountTable.Columns.IsDeleted} = 0;
            """;
        updateCommand.Parameters.AddWithValue("@UserId", userId);
        updateCommand.Parameters.AddWithValue("@PasswordHash", passwordHash);

        if (await updateCommand.ExecuteNonQueryAsync(cancellationToken) != 1)
        {
            throw new ApplicationServiceException(ServiceErrorDefinitions.Users.UserNotFound);
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
                {UserAccountTable.Columns.PhoneNumber},
                {UserAccountTable.Columns.PasswordSetupRequired},
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
            throw new ApplicationServiceException(ServiceStatusErrorCodes.InternalServerError, "User account could not be loaded.");
        }

        return new UserAccount(
            userId,
            Guid.Parse(reader.GetString("UserUuid")),
            reader.GetString(UserAccountTable.Columns.Username),
            reader.GetNullableString(UserAccountTable.Columns.DisplayName),
            reader.GetBoolean(reader.GetOrdinal(UserAccountTable.Columns.EmailVerified)),
            reader.GetBoolean(reader.GetOrdinal(UserAccountTable.Columns.IsActive)),
            reader.GetBoolean(reader.GetOrdinal(UserAccountTable.Columns.IsDeleted)),
            reader.GetUtcDateTime(UserAccountTable.Columns.CreatedAt))
        {
            PhoneNumber = reader.GetNullableString(UserAccountTable.Columns.PhoneNumber),
            PasswordSetupRequired = reader.GetBoolean(reader.GetOrdinal(UserAccountTable.Columns.PasswordSetupRequired))
        };
    }

    public async Task<UserAccount> GetByUuidAsync(
        Guid userUuid,
        CancellationToken cancellationToken)
    {
        var databaseTransaction = CurrentTransaction();
        var command = databaseTransaction.Connection.CreateCommand();
        command.Transaction = databaseTransaction.Transaction;
        command.CommandText =
            $"""
            SELECT
                {UserAccountTable.Columns.UserId},
                BIN_TO_UUID({UserAccountTable.Columns.UserUuid}) AS UserUuid,
                {UserAccountTable.Columns.Username},
                {UserAccountTable.Columns.DisplayName},
                {UserAccountTable.Columns.PhoneNumber},
                {UserAccountTable.Columns.PasswordSetupRequired},
                {UserAccountTable.Columns.EmailVerified},
                {UserAccountTable.Columns.IsActive},
                {UserAccountTable.Columns.IsDeleted},
                {UserAccountTable.Columns.CreatedAt}
            FROM {UserAccountTable.Name}
            WHERE {UserAccountTable.Columns.UserUuid} = UUID_TO_BIN(@UserUuid)
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@UserUuid", userUuid.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new ApplicationServiceException(ServiceErrorDefinitions.Users.UserNotFound);
        }

        return new UserAccount(
            reader.GetInt64(reader.GetOrdinal(UserAccountTable.Columns.UserId)),
            Guid.Parse(reader.GetString("UserUuid")),
            reader.GetString(UserAccountTable.Columns.Username),
            reader.GetNullableString(UserAccountTable.Columns.DisplayName),
            reader.GetBoolean(reader.GetOrdinal(UserAccountTable.Columns.EmailVerified)),
            reader.GetBoolean(reader.GetOrdinal(UserAccountTable.Columns.IsActive)),
            reader.GetBoolean(reader.GetOrdinal(UserAccountTable.Columns.IsDeleted)),
            reader.GetUtcDateTime(UserAccountTable.Columns.CreatedAt))
        {
            PhoneNumber = reader.GetNullableString(UserAccountTable.Columns.PhoneNumber),
            PasswordSetupRequired = reader.GetBoolean(reader.GetOrdinal(UserAccountTable.Columns.PasswordSetupRequired))
        };
    }

    private MySqlDatabaseTransaction CurrentTransaction() =>
        _transactionAccessor.GetCurrent().AsMySql();
}

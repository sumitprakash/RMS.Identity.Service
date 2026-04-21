using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.SignUp;
using RMS.Identity.Service.Domain.Entities.SignUp;
using RMS.Identity.Service.Domain.Interfaces.SignUp;
using RMS.Identity.Service.Infrastructure.Data;

namespace RMS.Identity.Service.Infrastructure.Persistence.SignUp;

public sealed class SignUpMySqlStore : ISignUpStore
{
    private const string Route = "/api/v1/signup";
    private const string Method = "POST";

    private readonly IMySqlConnectionFactory _connectionFactory;
    private readonly ILogger<SignUpMySqlStore> _logger;

    public SignUpMySqlStore(IMySqlConnectionFactory connectionFactory, ILogger<SignUpMySqlStore> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<SignUpUser> ExecuteAsync(SignUpStorageCommand command, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        SignUpUser? existingResponse = null;
        if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            existingResponse = await TryGetStoredResponseAsync(connection, transaction, command, cancellationToken);
            if (existingResponse is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                return existingResponse;
            }

            existingResponse = await ReserveIdempotencyKeyAsync(connection, transaction, command, cancellationToken);
            if (existingResponse is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                return existingResponse;
            }
        }

        if (await UsernameExistsAsync(connection, transaction, command.Username, cancellationToken))
        {
            throw new ServiceException((int)HttpStatusCode.Conflict, "USER_EXISTS", "Username already exists.");
        }

        long userId;
        try
        {
            userId = await InsertUserAsync(connection, transaction, command, cancellationToken);
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            throw new ServiceException((int)HttpStatusCode.Conflict, "USER_EXISTS", "Username already exists.");
        }

        await InsertEmailVerificationAsync(connection, transaction, userId, command, cancellationToken);

        var createdUser = await LoadCreatedUserAsync(connection, transaction, userId, cancellationToken);
        await InsertAuditLogAsync(connection, transaction, createdUser, cancellationToken);

        if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            await StoreIdempotentResponseAsync(connection, transaction, command.IdempotencyKey, createdUser, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        await TryInsertVerificationEmailOutboxAsync(connection, command, createdUser, cancellationToken);

        return createdUser;
    }

    private static async Task<SignUpUser?> TryGetStoredResponseAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        SignUpStorageCommand command,
        CancellationToken cancellationToken,
        bool lockForUpdate = false)
    {
        var selectCommand = connection.CreateCommand();
        selectCommand.Transaction = transaction;
        selectCommand.CommandText =
            $"""
            SELECT Method, Route, RequestHash, ResponseCode, CAST(ResponseBody AS CHAR) AS ResponseBody
            FROM IdempotencyKey
            WHERE KeyValue = @KeyValue
            LIMIT 1
            {GetLockClause(lockForUpdate)};
            """;
        selectCommand.Parameters.AddWithValue("@KeyValue", command.IdempotencyKey);

        await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var existingMethod = reader.GetString("Method");
        var existingRoute = reader.GetString("Route");
        var existingRequestHash = reader.GetNullableString("RequestHash");
        int? responseCode = reader.IsDBNull(reader.GetOrdinal("ResponseCode")) ? null : reader.GetInt32("ResponseCode");
        var responseBody = reader.GetNullableString("ResponseBody");

        if (!string.Equals(existingMethod, Method, StringComparison.OrdinalIgnoreCase) || !string.Equals(existingRoute, Route, StringComparison.Ordinal))
        {
            throw new ServiceException((int)HttpStatusCode.Conflict, "IDEMPOTENCY_CONFLICT", "Idempotency key was already used for a different request.");
        }

        if (!string.Equals(existingRequestHash, command.RequestHash, StringComparison.Ordinal))
        {
            throw new ServiceException((int)HttpStatusCode.Conflict, "IDEMPOTENCY_CONFLICT", "Idempotency key payload does not match the original request.");
        }

        if (responseCode is null || string.IsNullOrWhiteSpace(responseBody))
        {
            throw new ServiceException((int)HttpStatusCode.Conflict, "IDEMPOTENCY_IN_PROGRESS", "Idempotent request is already in progress.");
        }

        return JsonSerializer.Deserialize<SignUpUser>(responseBody);
    }

    private static async Task<SignUpUser?> ReserveIdempotencyKeyAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        SignUpStorageCommand command,
        CancellationToken cancellationToken)
    {
        var insertCommand = connection.CreateCommand();
        insertCommand.Transaction = transaction;
        insertCommand.CommandText =
            """
            INSERT INTO IdempotencyKey (KeyValue, Method, Route, RequestHash)
            VALUES (@KeyValue, @Method, @Route, @RequestHash);
            """;
        insertCommand.Parameters.AddWithValue("@KeyValue", command.IdempotencyKey);
        insertCommand.Parameters.AddWithValue("@Method", Method);
        insertCommand.Parameters.AddWithValue("@Route", Route);
        insertCommand.Parameters.AddWithValue("@RequestHash", (object?)command.RequestHash ?? DBNull.Value);

        try
        {
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            var existingResponse = await TryGetStoredResponseAsync(
                connection,
                transaction,
                command,
                cancellationToken,
                lockForUpdate: true);

            return existingResponse
                ?? throw new ServiceException((int)HttpStatusCode.Conflict, "IDEMPOTENCY_IN_PROGRESS", "Idempotent request is already in progress.");
        }

        return null;
    }

    private static string GetLockClause(bool lockForUpdate) => lockForUpdate ? "FOR UPDATE" : string.Empty;

    private static async Task<bool> UsernameExistsAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        string username,
        CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
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

    private static async Task<long> InsertUserAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        SignUpStorageCommand command,
        CancellationToken cancellationToken)
    {
        var insertCommand = connection.CreateCommand();
        insertCommand.Transaction = transaction;
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

        await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        return insertCommand.LastInsertedId;
    }

    private static async Task InsertEmailVerificationAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        long userId,
        SignUpStorageCommand command,
        CancellationToken cancellationToken)
    {
        var insertCommand = connection.CreateCommand();
        insertCommand.Transaction = transaction;
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
        insertCommand.Parameters.AddWithValue("@UserId", userId);
        insertCommand.Parameters.AddWithValue("@TokenHash", command.VerificationTokenHash);
        insertCommand.Parameters.AddWithValue("@ExpiresAt", command.VerificationTokenExpiresAt);
        await insertCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<SignUpUser> LoadCreatedUserAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        long userId,
        CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT
                BIN_TO_UUID(UserUUID) AS UserUuid,
                Username,
                DisplayName,
                CreatedAt
            FROM UserAccount
            WHERE UserID = @UserId
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@UserId", userId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new ServiceException((int)HttpStatusCode.InternalServerError, "SIGNUP_READ_FAILED", "Created user could not be loaded.");
        }

        return new SignUpUser(
            Guid.Parse(reader.GetString("UserUuid")),
            reader.GetString("Username"),
            reader.GetNullableString("DisplayName"),
            "pending",
            reader.GetUtcDateTime("CreatedAt"));
    }

    private static async Task InsertAuditLogAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        SignUpUser createdUser,
        CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO AuditLog (TableName, RecordId, Action, Payload, CreatedAt)
            VALUES ('UserAccount', @RecordId, 'signup_created', CAST(@Payload AS JSON), UTC_TIMESTAMP());
            """;
        command.Parameters.AddWithValue("@RecordId", createdUser.UserUuid.ToString());
        command.Parameters.AddWithValue("@Payload", JsonSerializer.Serialize(createdUser));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task TryInsertVerificationEmailOutboxAsync(
        MySqlConnection connection,
        SignUpStorageCommand command,
        SignUpUser createdUser,
        CancellationToken cancellationToken)
    {
        try
        {
            await InsertVerificationEmailOutboxAsync(connection, null, command, createdUser, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Signup committed but failed to enqueue verification email for user {UserUuid}.", createdUser.UserUuid);
        }
    }

    private static async Task InsertVerificationEmailOutboxAsync(
        MySqlConnection connection,
        MySqlTransaction? transaction,
        SignUpStorageCommand command,
        SignUpUser createdUser,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            userUuid = createdUser.UserUuid,
            username = createdUser.Username,
            displayName = createdUser.DisplayName,
            verificationToken = command.VerificationToken,
            purpose = "email_verification"
        });

        var insertCommand = connection.CreateCommand();
        insertCommand.Transaction = transaction;
        insertCommand.CommandText =
            """
            INSERT INTO Outbox (
                EventType,
                AggregateType,
                AggregateUUID,
                Payload,
                Status,
                Retries,
                CreatedAt,
                AvailableAt)
            VALUES (
                'identity.email_verification_requested',
                'UserAccount',
                UUID_TO_BIN(@UserUuid),
                CAST(@Payload AS JSON),
                'pending',
                0,
                UTC_TIMESTAMP(),
                UTC_TIMESTAMP());
            """;
        insertCommand.Parameters.AddWithValue("@UserUuid", createdUser.UserUuid.ToString());
        insertCommand.Parameters.AddWithValue("@Payload", payload);
        await insertCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task StoreIdempotentResponseAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        string idempotencyKey,
        SignUpUser createdUser,
        CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            UPDATE IdempotencyKey
            SET ResponseCode = 201,
                ResponseBody = CAST(@ResponseBody AS JSON)
            WHERE KeyValue = @KeyValue;
            """;
        command.Parameters.AddWithValue("@KeyValue", idempotencyKey);
        command.Parameters.AddWithValue("@ResponseBody", JsonSerializer.Serialize(createdUser));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

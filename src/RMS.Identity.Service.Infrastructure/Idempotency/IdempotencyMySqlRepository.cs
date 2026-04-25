using MySqlConnector;
using RMS.Identity.Service.Domain.Contracts.Idempotency;
using RMS.Identity.Service.Domain.Interfaces.Idempotency;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Infrastructure.Data;
using RMS.Identity.Service.Infrastructure.Persistence.SignUp;

namespace RMS.Identity.Service.Infrastructure.Idempotency;

public sealed class IdempotencyMySqlRepository : IIdempotencyRepository
{
    public async Task<IdempotencyRecord?> GetAsync(
        IDatabaseTransaction transaction,
        string key,
        bool lockForUpdate,
        CancellationToken cancellationToken)
    {
        var databaseTransaction = transaction.AsMySql();
        var command = databaseTransaction.Connection.CreateCommand();
        command.Transaction = databaseTransaction.Transaction;
        command.CommandText =
            $"""
            SELECT Method, Route, RequestHash, ResponseCode, CAST(ResponseBody AS CHAR) AS ResponseBody
            FROM IdempotencyKey
            WHERE KeyValue = @KeyValue
            LIMIT 1
            {GetLockClause(lockForUpdate)};
            """;
        command.Parameters.AddWithValue("@KeyValue", key);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new IdempotencyRecord(
            reader.GetString("Method"),
            reader.GetString("Route"),
            reader.GetNullableString("RequestHash"),
            reader.IsDBNull(reader.GetOrdinal("ResponseCode")) ? null : reader.GetInt32("ResponseCode"),
            reader.GetNullableString("ResponseBody"));
    }

    public async Task<bool> TryCreateAsync(
        IDatabaseTransaction transaction,
        IdempotencyRequest request,
        CancellationToken cancellationToken)
    {
        var databaseTransaction = transaction.AsMySql();
        var command = databaseTransaction.Connection.CreateCommand();
        command.Transaction = databaseTransaction.Transaction;
        command.CommandText =
            """
            INSERT INTO IdempotencyKey (KeyValue, Method, Route, RequestHash)
            VALUES (@KeyValue, @Method, @Route, @RequestHash);
            """;
        command.Parameters.AddWithValue("@KeyValue", request.Key);
        command.Parameters.AddWithValue("@Method", request.Method);
        command.Parameters.AddWithValue("@Route", request.Route);
        command.Parameters.AddWithValue("@RequestHash", (object?)request.RequestHash ?? DBNull.Value);

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
            return true;
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            return false;
        }
    }

    public async Task StoreResponseAsync(
        IDatabaseTransaction transaction,
        string key,
        int responseCode,
        string responseBody,
        CancellationToken cancellationToken)
    {
        var databaseTransaction = transaction.AsMySql();
        var command = databaseTransaction.Connection.CreateCommand();
        command.Transaction = databaseTransaction.Transaction;
        command.CommandText =
            """
            UPDATE IdempotencyKey
            SET ResponseCode = @ResponseCode,
                ResponseBody = CAST(@ResponseBody AS JSON)
            WHERE KeyValue = @KeyValue;
            """;
        command.Parameters.AddWithValue("@KeyValue", key);
        command.Parameters.AddWithValue("@ResponseCode", responseCode);
        command.Parameters.AddWithValue("@ResponseBody", responseBody);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string GetLockClause(bool lockForUpdate) => lockForUpdate ? "FOR UPDATE" : string.Empty;
}

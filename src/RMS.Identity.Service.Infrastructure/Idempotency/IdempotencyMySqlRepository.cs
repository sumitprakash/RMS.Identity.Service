using MySqlConnector;
using RMS.Identity.Service.Domain.Contracts.Idempotency;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Idempotency;
using RMS.Identity.Service.Infrastructure.Data;
using RMS.Identity.Service.Infrastructure.Persistence;
using RMS.Identity.Service.Infrastructure.Persistence.Schema;

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
            SELECT
                {IdempotencyKeyTable.Columns.Method},
                {IdempotencyKeyTable.Columns.Route},
                {IdempotencyKeyTable.Columns.RequestHash},
                {IdempotencyKeyTable.Columns.ResponseCode},
                CAST({IdempotencyKeyTable.Columns.ResponseBody} AS CHAR) AS {IdempotencyKeyTable.Columns.ResponseBody}
            FROM {IdempotencyKeyTable.Name}
            WHERE {IdempotencyKeyTable.Columns.KeyValue} = @KeyValue
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
            reader.GetString(IdempotencyKeyTable.Columns.Method),
            reader.GetString(IdempotencyKeyTable.Columns.Route),
            reader.GetNullableString(IdempotencyKeyTable.Columns.RequestHash),
            reader.IsDBNull(reader.GetOrdinal(IdempotencyKeyTable.Columns.ResponseCode))
                ? null
                : reader.GetInt32(IdempotencyKeyTable.Columns.ResponseCode),
            reader.GetNullableString(IdempotencyKeyTable.Columns.ResponseBody));
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
            $"""
            INSERT INTO {IdempotencyKeyTable.Name} (
                {IdempotencyKeyTable.Columns.KeyValue},
                {IdempotencyKeyTable.Columns.Method},
                {IdempotencyKeyTable.Columns.Route},
                {IdempotencyKeyTable.Columns.RequestHash})
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
            $"""
            UPDATE {IdempotencyKeyTable.Name}
            SET {IdempotencyKeyTable.Columns.ResponseCode} = @ResponseCode,
                {IdempotencyKeyTable.Columns.ResponseBody} = CAST(@ResponseBody AS JSON)
            WHERE {IdempotencyKeyTable.Columns.KeyValue} = @KeyValue;
            """;
        command.Parameters.AddWithValue("@KeyValue", key);
        command.Parameters.AddWithValue("@ResponseCode", responseCode);
        command.Parameters.AddWithValue("@ResponseBody", responseBody);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string GetLockClause(bool lockForUpdate) => lockForUpdate ? "FOR UPDATE" : string.Empty;
}

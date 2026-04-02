using Dapper;
using RMS.Identity.Service.Domain.Entities;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Infrastructure.Persistence.Internal;

namespace RMS.Identity.Service.Infrastructure.Persistence;

internal sealed class IdempotencyRepository : RepositoryBase, IIdempotencyRepository
{
    public IdempotencyRepository(MySqlConnectionFactory connectionFactory, DbSessionAccessor sessionAccessor)
        : base(connectionFactory, sessionAccessor)
    {
    }

    public Task<IdempotencyKeyRecord?> GetAsync(string keyValue, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                IdempotencyKeyID,
                KeyValue,
                Method,
                Route,
                RequestHash,
                ResponseCode,
                CAST(ResponseBody AS CHAR) AS ResponseBody,
                CreatedAt
            FROM IdempotencyKey
            WHERE KeyValue = @KeyValue
            LIMIT 1;
            """;

        return WithConnectionAsync(
            (connection, transaction) => connection.QuerySingleOrDefaultAsync<IdempotencyKeyRecord>(
                new CommandDefinition(sql, new { KeyValue = keyValue }, transaction, cancellationToken: cancellationToken)),
            cancellationToken);
    }

    public Task CreateAsync(IdempotencyKeyRecord record, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO IdempotencyKey
            (
                KeyValue,
                Method,
                Route,
                RequestHash,
                ResponseCode,
                ResponseBody,
                CreatedAt
            )
            VALUES
            (
                @KeyValue,
                @Method,
                @Route,
                @RequestHash,
                @ResponseCode,
                @ResponseBody,
                @CreatedAt
            );
            """;

        return WithConnectionAsync(
            (connection, transaction) => connection.ExecuteAsync(
                new CommandDefinition(sql, record, transaction, cancellationToken: cancellationToken)),
            cancellationToken);
    }
}

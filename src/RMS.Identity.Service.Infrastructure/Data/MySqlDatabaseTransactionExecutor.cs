using RMS.Identity.Service.Domain.Interfaces.Persistence;

namespace RMS.Identity.Service.Infrastructure.Data;

public sealed class MySqlDatabaseTransactionExecutor : IDatabaseTransactionExecutor
{
    private readonly IMySqlConnectionFactory _connectionFactory;

    public MySqlDatabaseTransactionExecutor(IMySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<TResult> ExecuteAsync<TResult>(
        Func<IDatabaseTransaction, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var databaseTransaction = new MySqlDatabaseTransaction(connection, transaction);

        try
        {
            var result = await operation(databaseTransaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

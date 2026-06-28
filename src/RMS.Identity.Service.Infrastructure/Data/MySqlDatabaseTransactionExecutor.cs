using RMS.Identity.Service.Domain.Interfaces.Persistence;

namespace RMS.Identity.Service.Infrastructure.Data;

public sealed class MySqlDatabaseTransactionExecutor : IDatabaseTransactionExecutor
{
    private readonly IMySqlConnectionFactory _connectionFactory;
    private readonly IDatabaseTransactionAccessor _transactionAccessor;

    public MySqlDatabaseTransactionExecutor(
        IMySqlConnectionFactory connectionFactory,
        IDatabaseTransactionAccessor transactionAccessor)
    {
        _connectionFactory = connectionFactory;
        _transactionAccessor = transactionAccessor;
    }

    public async Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        if (_transactionAccessor.Current is not null)
        {
            return await operation(cancellationToken);
        }

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var databaseTransaction = new MySqlDatabaseTransaction(connection, transaction);
        var previousTransaction = _transactionAccessor.Current;
        _transactionAccessor.Current = databaseTransaction;

        try
        {
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            _transactionAccessor.Current = previousTransaction;
        }
    }
}

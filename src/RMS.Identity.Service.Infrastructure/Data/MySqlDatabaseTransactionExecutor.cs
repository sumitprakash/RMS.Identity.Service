using Microsoft.Extensions.Logging;
using RMS.Identity.Service.Domain.Interfaces.Persistence;

namespace RMS.Identity.Service.Infrastructure.Data;

public sealed class MySqlDatabaseTransactionExecutor : IDatabaseTransactionExecutor
{
    private readonly IMySqlConnectionFactory _connectionFactory;
    private readonly IDatabaseTransactionAccessor _transactionAccessor;
    private readonly ILogger<MySqlDatabaseTransactionExecutor> _logger;

    public MySqlDatabaseTransactionExecutor(
        IMySqlConnectionFactory connectionFactory,
        IDatabaseTransactionAccessor transactionAccessor,
        ILogger<MySqlDatabaseTransactionExecutor> logger)
    {
        _connectionFactory = connectionFactory;
        _transactionAccessor = transactionAccessor;
        _logger = logger;
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
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            try
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            catch (Exception rollbackException)
            {
                _logger.LogError(
                    rollbackException,
                    "Failed to roll back MySQL transaction after operation failure.");
            }

            throw;
        }
        finally
        {
            _transactionAccessor.Current = previousTransaction;
        }
    }
}

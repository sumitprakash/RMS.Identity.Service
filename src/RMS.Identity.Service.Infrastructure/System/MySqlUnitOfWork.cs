using RMS.Identity.Service.Domain.Interfaces.System;
using RMS.Identity.Service.Infrastructure.Persistence.Internal;

namespace RMS.Identity.Service.Infrastructure.System;

internal sealed class MySqlUnitOfWork : IUnitOfWork
{
    private readonly MySqlConnectionFactory _connectionFactory;
    private readonly DbSessionAccessor _sessionAccessor;

    public MySqlUnitOfWork(MySqlConnectionFactory connectionFactory, DbSessionAccessor sessionAccessor)
    {
        _connectionFactory = connectionFactory;
        _sessionAccessor = sessionAccessor;
    }

    public Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync<object?>(
            async ct =>
            {
                await action(ct);
                return null;
            },
            cancellationToken);
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    {
        if (_sessionAccessor.Current is not null)
        {
            return await action(cancellationToken);
        }

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        _sessionAccessor.Current = new DbSession
        {
            Connection = connection,
            Transaction = transaction
        };

        try
        {
            var result = await action(cancellationToken);
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
            _sessionAccessor.Current = null;
        }
    }
}

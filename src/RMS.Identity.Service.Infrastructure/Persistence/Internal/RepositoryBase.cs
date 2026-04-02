using MySqlConnector;

namespace RMS.Identity.Service.Infrastructure.Persistence.Internal;

internal abstract class RepositoryBase
{
    private readonly MySqlConnectionFactory _connectionFactory;
    private readonly DbSessionAccessor _sessionAccessor;

    protected RepositoryBase(MySqlConnectionFactory connectionFactory, DbSessionAccessor sessionAccessor)
    {
        _connectionFactory = connectionFactory;
        _sessionAccessor = sessionAccessor;
    }

    protected async Task<T> WithConnectionAsync<T>(
        Func<MySqlConnection, MySqlTransaction?, Task<T>> action,
        CancellationToken cancellationToken)
    {
        var session = _sessionAccessor.Current;
        if (session is not null)
        {
            return await action(session.Connection, session.Transaction);
        }

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        return await action(connection, null);
    }

    protected async Task WithConnectionAsync(
        Func<MySqlConnection, MySqlTransaction?, Task> action,
        CancellationToken cancellationToken)
    {
        var session = _sessionAccessor.Current;
        if (session is not null)
        {
            await action(session.Connection, session.Transaction);
            return;
        }

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await action(connection, null);
    }

    protected async Task WithConnectionAsync(
        Func<MySqlConnection, MySqlTransaction?, Task<int>> action,
        CancellationToken cancellationToken)
    {
        var session = _sessionAccessor.Current;
        if (session is not null)
        {
            await action(session.Connection, session.Transaction);
            return;
        }

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await action(connection, null);
    }
}

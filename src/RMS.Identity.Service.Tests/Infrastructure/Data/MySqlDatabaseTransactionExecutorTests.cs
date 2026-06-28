using MySqlConnector;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Infrastructure.Data;

namespace RMS.Identity.Service.Tests.Infrastructure.Data;

public sealed class MySqlDatabaseTransactionExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_WithAmbientTransaction_JoinsExistingTransaction()
    {
        var accessor = new FakeTransactionAccessor { Current = new FakeTransaction() };
        var connectionFactory = new ThrowingConnectionFactory();
        var executor = new MySqlDatabaseTransactionExecutor(connectionFactory, accessor);

        var result = await executor.ExecuteAsync(
            _ => Task.FromResult("completed"),
            CancellationToken.None);

        Assert.Equal("completed", result);
        Assert.Equal(0, connectionFactory.OpenCount);
        Assert.IsType<FakeTransaction>(accessor.Current);
    }

    private sealed class ThrowingConnectionFactory : IMySqlConnectionFactory
    {
        public int OpenCount { get; private set; }

        public Task<MySqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            OpenCount++;
            throw new InvalidOperationException("A nested transaction must not open another connection.");
        }
    }

    private sealed class FakeTransactionAccessor : IDatabaseTransactionAccessor
    {
        public IDatabaseTransaction? Current { get; set; }

        public IDatabaseTransaction GetCurrent() =>
            Current ?? throw new InvalidOperationException();
    }

    private sealed class FakeTransaction : IDatabaseTransaction;
}

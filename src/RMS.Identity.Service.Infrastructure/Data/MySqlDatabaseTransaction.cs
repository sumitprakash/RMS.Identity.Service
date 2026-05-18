using MySqlConnector;
using RMS.Identity.Service.Domain.Interfaces.Persistence;

namespace RMS.Identity.Service.Infrastructure.Data;

internal sealed class MySqlDatabaseTransaction : IDatabaseTransaction
{
    public MySqlDatabaseTransaction(MySqlConnection connection, MySqlTransaction transaction)
    {
        Connection = connection;
        Transaction = transaction;
    }

    internal MySqlConnection Connection { get; }

    internal MySqlTransaction Transaction { get; }
}

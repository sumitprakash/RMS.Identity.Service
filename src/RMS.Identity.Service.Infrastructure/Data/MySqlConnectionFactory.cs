using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace RMS.Identity.Service.Infrastructure.Data;

public sealed class MySqlConnectionFactory : IMySqlConnectionFactory
{
    private readonly string _connectionString;

    public MySqlConnectionFactory(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'Default' is required. Set the ConnectionStrings__Default environment variable.");
        }

        _connectionString = connectionString;
    }

    public async Task<MySqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}

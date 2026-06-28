using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace RMS.Identity.Service.Infrastructure.Data;

public sealed class MySqlConnectionFactory : IMySqlConnectionFactory
{
    private readonly string _connectionString;
    private readonly ILogger<MySqlConnectionFactory> _logger;

    public MySqlConnectionFactory(
        IConfiguration configuration,
        ILogger<MySqlConnectionFactory> logger)
    {
        _logger = logger;
        var connectionString = configuration.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogCritical("Connection string 'Default' is missing.");
            throw new InvalidOperationException(
                "Connection string 'Default' is required. Set the ConnectionStrings__Default environment variable.");
        }

        _connectionString = connectionString;
    }

    public async Task<MySqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Failed to open MySQL connection.");
            throw;
        }
    }
}

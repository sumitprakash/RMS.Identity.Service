using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace RMS.Identity.Service.Tests.Shared;

public class TestDatabaseWebApplicationFactory : WebApplicationFactory<Program>
{
    public string ConnectionString { get; } = CreateConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ConnectionString
            });
        });
    }

    public async Task<bool> IsDatabaseAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenDatabaseConnectionAsync(cancellationToken);
            return true;
        }
        catch (MySqlException)
        {
            return false;
        }
    }

    public async Task<bool> HasCompanySchemaAsync(CancellationToken cancellationToken = default)
    {
        if (!await IsDatabaseAvailableAsync(cancellationToken))
        {
            return false;
        }

        await using var connection = await OpenDatabaseConnectionAsync(cancellationToken);
        return await HasTableAsync(connection, "CompanyUser", cancellationToken)
            && await HasColumnAsync(connection, "Company", "LegalName", cancellationToken)
            && await HasColumnAsync(connection, "Company", "CompanyGSTIN", cancellationToken);
    }

    public async Task<MySqlConnection> OpenDatabaseConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static string CreateConnectionString()
    {
        var configuredConnectionString =
            Environment.GetEnvironmentVariable("RMS_IDENTITY_TEST_CONNECTION_STRING")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Server=127.0.0.1;Port=3306;Database=rms_identity;User ID=rms_user;Password=12345678;";

        var builder = new MySqlConnectionStringBuilder(configuredConnectionString)
        {
            SslMode = MySqlSslMode.None,
            ConnectionTimeout = 2
        };

        return builder.ConnectionString;
    }

    private static async Task<bool> HasTableAsync(
        MySqlConnection connection,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT 1
            FROM information_schema.TABLES
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = @TableName
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@TableName", tableName);
        return await command.ExecuteScalarAsync(cancellationToken) is not null;
    }

    private static async Task<bool> HasColumnAsync(
        MySqlConnection connection,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT 1
            FROM information_schema.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = @TableName
              AND COLUMN_NAME = @ColumnName
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@TableName", tableName);
        command.Parameters.AddWithValue("@ColumnName", columnName);
        return await command.ExecuteScalarAsync(cancellationToken) is not null;
    }
}

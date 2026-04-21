using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace RMS.Identity.Service.Tests.Endpoint.SignUp;

public sealed class SignUpWebApplicationFactory : WebApplicationFactory<Program>
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
            SslMode = MySqlSslMode.None
        };

        return builder.ConnectionString;
    }
}

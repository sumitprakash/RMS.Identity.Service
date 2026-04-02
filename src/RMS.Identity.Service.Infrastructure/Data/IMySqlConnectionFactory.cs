using MySqlConnector;

namespace RMS.Identity.Service.Infrastructure.Data;

public interface IMySqlConnectionFactory
{
    Task<MySqlConnection> OpenConnectionAsync(CancellationToken cancellationToken);
}

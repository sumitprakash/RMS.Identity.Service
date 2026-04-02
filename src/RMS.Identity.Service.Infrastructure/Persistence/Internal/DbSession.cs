using MySqlConnector;

namespace RMS.Identity.Service.Infrastructure.Persistence.Internal;

internal sealed class DbSession
{
    public required MySqlConnection Connection { get; init; }

    public required MySqlTransaction Transaction { get; init; }
}

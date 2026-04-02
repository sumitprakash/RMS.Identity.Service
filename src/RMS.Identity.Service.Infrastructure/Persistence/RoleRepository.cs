using Dapper;
using RMS.Identity.Service.Domain.Entities;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Infrastructure.Persistence.Internal;

namespace RMS.Identity.Service.Infrastructure.Persistence;

internal sealed class RoleRepository : RepositoryBase, IRoleRepository
{
    public RoleRepository(MySqlConnectionFactory connectionFactory, DbSessionAccessor sessionAccessor)
        : base(connectionFactory, sessionAccessor)
    {
    }

    public async Task<IReadOnlyList<Role>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                r.RoleID,
                BIN_TO_UUID(r.RoleUUID) AS RoleUUID,
                r.Name,
                r.Description,
                r.IsSystemRole,
                r.IsDeleted,
                r.CreatedAt
            FROM Role r
            INNER JOIN UserRole ur ON ur.RoleID = r.RoleID
            WHERE ur.UserID = @UserID
              AND r.IsDeleted = 0
            ORDER BY r.Name;
            """;

        var roles = await WithConnectionAsync(
            (connection, transaction) => connection.QueryAsync<Role>(
                new CommandDefinition(sql, new { UserID = userId }, transaction, cancellationToken: cancellationToken)),
            cancellationToken);

        return roles.ToArray();
    }
}

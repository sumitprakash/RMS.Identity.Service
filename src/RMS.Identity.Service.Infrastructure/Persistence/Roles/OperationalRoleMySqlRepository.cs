using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Roles;
using RMS.Identity.Service.Infrastructure.Data;
using RMS.Identity.Service.Infrastructure.Persistence.Schema;

namespace RMS.Identity.Service.Infrastructure.Persistence.Roles;

public sealed class OperationalRoleMySqlRepository : IOperationalRoleReadRepository
{
    private readonly IDatabaseTransactionAccessor _transactionAccessor;

    public OperationalRoleMySqlRepository(IDatabaseTransactionAccessor transactionAccessor)
    {
        _transactionAccessor = transactionAccessor;
    }

    public async Task<bool> UserHasAnyRoleAsync(
        Guid userUuid,
        IReadOnlyCollection<string> roleNames,
        CancellationToken cancellationToken)
    {
        if (roleNames.Count == 0)
        {
            return false;
        }

        var databaseTransaction = _transactionAccessor.GetCurrent().AsMySql();
        var command = databaseTransaction.Connection.CreateCommand();
        command.Transaction = databaseTransaction.Transaction;
        var roleParameters = roleNames
            .Select((_, index) => $"@RoleName{index}")
            .ToArray();
        command.CommandText =
            $"""
            SELECT 1
            FROM {UserAccountTable.Name} ua
            INNER JOIN {UserRoleTable.Name} ur
                ON ur.{UserRoleTable.Columns.UserId} = ua.{UserAccountTable.Columns.UserId}
            INNER JOIN {RoleTable.Name} r
                ON r.{RoleTable.Columns.RoleId} = ur.{UserRoleTable.Columns.RoleId}
            WHERE ua.{UserAccountTable.Columns.UserUuid} = UUID_TO_BIN(@UserUuid)
              AND ua.{UserAccountTable.Columns.IsDeleted} = 0
              AND ua.{UserAccountTable.Columns.IsActive} = 1
              AND r.{RoleTable.Columns.IsDeleted} = 0
              AND r.{RoleTable.Columns.Name} IN ({string.Join(", ", roleParameters)})
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@UserUuid", userUuid.ToString());
        for (var index = 0; index < roleNames.Count; index++)
        {
            command.Parameters.AddWithValue(roleParameters[index], roleNames.ElementAt(index));
        }

        return await command.ExecuteScalarAsync(cancellationToken) is not null;
    }
}

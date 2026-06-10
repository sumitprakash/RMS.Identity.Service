using RMS.Identity.Service.Domain.Entities.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Companies;
using RMS.Identity.Service.Infrastructure.Data;
using RMS.Identity.Service.Infrastructure.Persistence.Schema;

namespace RMS.Identity.Service.Infrastructure.Persistence.Companies;

public sealed class CompanyMembershipMySqlRepository : ICompanyMembershipReadRepository
{
    private readonly IMySqlConnectionFactory _connectionFactory;

    public CompanyMembershipMySqlRepository(IMySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyCollection<UserCompanyMembership>> ListByUserUuidAsync(
        Guid userUuid,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
            SELECT
                BIN_TO_UUID(c.{CompanyTable.Columns.CompanyUuid}) AS CompanyUuid,
                c.{CompanyTable.Columns.LegalName},
                c.{CompanyTable.Columns.TradeName},
                c.{CompanyTable.Columns.CompanyGstin},
                c.{CompanyTable.Columns.CompanyStatus},
                cu.{CompanyUserTable.Columns.CompanyRole},
                cu.{CompanyUserTable.Columns.MembershipStatus},
                c.{CompanyTable.Columns.CreatedAt}
            FROM {UserAccountTable.Name} ua
            INNER JOIN {CompanyUserTable.Name} cu
                ON cu.{CompanyUserTable.Columns.UserId} = ua.{UserAccountTable.Columns.UserId}
            INNER JOIN {CompanyTable.Name} c
                ON c.{CompanyTable.Columns.CompanyId} = cu.{CompanyUserTable.Columns.CompanyId}
            WHERE ua.{UserAccountTable.Columns.UserUuid} = UUID_TO_BIN(@UserUuid)
              AND ua.{UserAccountTable.Columns.IsDeleted} = 0
              AND c.{CompanyTable.Columns.IsDeleted} = 0
              AND cu.{CompanyUserTable.Columns.MembershipStatus} <> 'suspended'
            ORDER BY c.{CompanyTable.Columns.CreatedAt} DESC;
            """;
        command.Parameters.AddWithValue("@UserUuid", userUuid.ToString());

        var companies = new List<UserCompanyMembership>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            companies.Add(new UserCompanyMembership(
                Guid.Parse(reader.GetString("CompanyUuid")),
                reader.GetString(CompanyTable.Columns.LegalName),
                reader.GetNullableString(CompanyTable.Columns.TradeName),
                reader.GetString(CompanyTable.Columns.CompanyGstin),
                reader.GetString(CompanyTable.Columns.CompanyStatus),
                reader.GetString(CompanyUserTable.Columns.CompanyRole),
                reader.GetString(CompanyUserTable.Columns.MembershipStatus),
                reader.GetUtcDateTime(CompanyTable.Columns.CreatedAt)));
        }

        return companies;
    }

    public async Task<CompanyMembership?> GetMembershipAsync(
        Guid userUuid,
        Guid companyUuid,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
            SELECT
                BIN_TO_UUID(ua.{UserAccountTable.Columns.UserUuid}) AS UserUuid,
                BIN_TO_UUID(c.{CompanyTable.Columns.CompanyUuid}) AS CompanyUuid,
                c.{CompanyTable.Columns.CompanyStatus},
                cu.{CompanyUserTable.Columns.CompanyRole},
                cu.{CompanyUserTable.Columns.MembershipStatus}
            FROM {UserAccountTable.Name} ua
            INNER JOIN {CompanyUserTable.Name} cu
                ON cu.{CompanyUserTable.Columns.UserId} = ua.{UserAccountTable.Columns.UserId}
            INNER JOIN {CompanyTable.Name} c
                ON c.{CompanyTable.Columns.CompanyId} = cu.{CompanyUserTable.Columns.CompanyId}
            WHERE ua.{UserAccountTable.Columns.UserUuid} = UUID_TO_BIN(@UserUuid)
              AND c.{CompanyTable.Columns.CompanyUuid} = UUID_TO_BIN(@CompanyUuid)
              AND ua.{UserAccountTable.Columns.IsDeleted} = 0
              AND c.{CompanyTable.Columns.IsDeleted} = 0
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@UserUuid", userUuid.ToString());
        command.Parameters.AddWithValue("@CompanyUuid", companyUuid.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new CompanyMembership(
            Guid.Parse(reader.GetString("UserUuid")),
            Guid.Parse(reader.GetString("CompanyUuid")),
            reader.GetString(CompanyTable.Columns.CompanyStatus),
            reader.GetString(CompanyUserTable.Columns.CompanyRole),
            reader.GetString(CompanyUserTable.Columns.MembershipStatus));
    }
}

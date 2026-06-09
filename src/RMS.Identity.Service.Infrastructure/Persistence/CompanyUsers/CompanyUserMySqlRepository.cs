using MySqlConnector;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.CompanyUsers;
using RMS.Identity.Service.Domain.Entities.Companies;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Domain.Interfaces.Repositories.CompanyUsers;
using RMS.Identity.Service.Infrastructure.Data;
using RMS.Identity.Service.Infrastructure.Persistence.Schema;
using System.Net;

namespace RMS.Identity.Service.Infrastructure.Persistence.CompanyUsers;

public sealed class CompanyUserMySqlRepository :
    ICompanyUserReadRepository,
    ICompanyUserWriteRepository
{
    private readonly IDatabaseTransactionAccessor _transactionAccessor;

    public CompanyUserMySqlRepository(IDatabaseTransactionAccessor transactionAccessor)
    {
        _transactionAccessor = transactionAccessor;
    }

    public async Task CreateAsync(
        CreateCompanyUserCommand command,
        CancellationToken cancellationToken)
    {
        var databaseTransaction = CurrentTransaction();
        var insertCommand = databaseTransaction.Connection.CreateCommand();
        insertCommand.Transaction = databaseTransaction.Transaction;
        insertCommand.CommandText =
            $"""
            INSERT INTO {CompanyUserTable.Name} (
                {CompanyUserTable.Columns.CompanyId},
                {CompanyUserTable.Columns.UserId},
                {CompanyUserTable.Columns.CompanyRole},
                {CompanyUserTable.Columns.MembershipStatus},
                {CompanyUserTable.Columns.JoinedAt},
                {CompanyUserTable.Columns.CreatedAt})
            VALUES (
                @CompanyId,
                @UserId,
                @CompanyRole,
                @MembershipStatus,
                UTC_TIMESTAMP(),
                UTC_TIMESTAMP());
            """;
        insertCommand.Parameters.AddWithValue("@CompanyId", command.CompanyId);
        insertCommand.Parameters.AddWithValue("@UserId", command.UserId);
        insertCommand.Parameters.AddWithValue("@CompanyRole", command.CompanyRole);
        insertCommand.Parameters.AddWithValue("@MembershipStatus", command.MembershipStatus);

        try
        {
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            throw new ServiceException(
                (int)HttpStatusCode.Conflict,
                "COMPANY_USER_EXISTS",
                "User already belongs to this company.");
        }
    }

    public async Task<CompanyUserAccount?> GetByCompanyAndUserUuidAsync(
        Guid companyUuid,
        Guid userUuid,
        CancellationToken cancellationToken)
    {
        var databaseTransaction = CurrentTransaction();
        var command = databaseTransaction.Connection.CreateCommand();
        command.Transaction = databaseTransaction.Transaction;
        command.CommandText =
            $"""
            SELECT
                BIN_TO_UUID(ua.{UserAccountTable.Columns.UserUuid}) AS UserUuid,
                ua.{UserAccountTable.Columns.Username},
                ua.{UserAccountTable.Columns.DisplayName},
                ua.{UserAccountTable.Columns.EmailVerified},
                ua.{UserAccountTable.Columns.IsActive},
                cu.{CompanyUserTable.Columns.CompanyRole},
                cu.{CompanyUserTable.Columns.MembershipStatus},
                ua.{UserAccountTable.Columns.CreatedAt}
            FROM {UserAccountTable.Name} ua
            INNER JOIN {CompanyUserTable.Name} cu
                ON cu.{CompanyUserTable.Columns.UserId} = ua.{UserAccountTable.Columns.UserId}
            INNER JOIN {CompanyTable.Name} c
                ON c.{CompanyTable.Columns.CompanyId} = cu.{CompanyUserTable.Columns.CompanyId}
            WHERE c.{CompanyTable.Columns.CompanyUuid} = UUID_TO_BIN(@CompanyUuid)
              AND ua.{UserAccountTable.Columns.UserUuid} = UUID_TO_BIN(@UserUuid)
              AND c.{CompanyTable.Columns.IsDeleted} = 0
              AND ua.{UserAccountTable.Columns.IsDeleted} = 0
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@CompanyUuid", companyUuid.ToString());
        command.Parameters.AddWithValue("@UserUuid", userUuid.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new CompanyUserAccount(
            Guid.Parse(reader.GetString("UserUuid")),
            reader.GetString(UserAccountTable.Columns.Username),
            reader.GetNullableString(UserAccountTable.Columns.DisplayName),
            reader.GetBoolean(reader.GetOrdinal(UserAccountTable.Columns.EmailVerified)),
            reader.GetBoolean(reader.GetOrdinal(UserAccountTable.Columns.IsActive)),
            reader.GetString(CompanyUserTable.Columns.CompanyRole),
            reader.GetString(CompanyUserTable.Columns.MembershipStatus),
            reader.GetUtcDateTime(UserAccountTable.Columns.CreatedAt));
    }

    public async Task<IReadOnlyCollection<CompanyUserAccount>> ListByCompanyUuidAsync(
        Guid companyUuid,
        CancellationToken cancellationToken)
    {
        var databaseTransaction = CurrentTransaction();
        var command = databaseTransaction.Connection.CreateCommand();
        command.Transaction = databaseTransaction.Transaction;
        command.CommandText =
            $"""
            SELECT
                BIN_TO_UUID(ua.{UserAccountTable.Columns.UserUuid}) AS UserUuid,
                ua.{UserAccountTable.Columns.Username},
                ua.{UserAccountTable.Columns.DisplayName},
                ua.{UserAccountTable.Columns.EmailVerified},
                ua.{UserAccountTable.Columns.IsActive},
                cu.{CompanyUserTable.Columns.CompanyRole},
                cu.{CompanyUserTable.Columns.MembershipStatus},
                ua.{UserAccountTable.Columns.CreatedAt}
            FROM {CompanyTable.Name} c
            INNER JOIN {CompanyUserTable.Name} cu
                ON cu.{CompanyUserTable.Columns.CompanyId} = c.{CompanyTable.Columns.CompanyId}
            INNER JOIN {UserAccountTable.Name} ua
                ON ua.{UserAccountTable.Columns.UserId} = cu.{CompanyUserTable.Columns.UserId}
            WHERE c.{CompanyTable.Columns.CompanyUuid} = UUID_TO_BIN(@CompanyUuid)
              AND c.{CompanyTable.Columns.IsDeleted} = 0
              AND ua.{UserAccountTable.Columns.IsDeleted} = 0
            ORDER BY ua.{UserAccountTable.Columns.Username} ASC;
            """;
        command.Parameters.AddWithValue("@CompanyUuid", companyUuid.ToString());

        var users = new List<CompanyUserAccount>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            users.Add(new CompanyUserAccount(
                Guid.Parse(reader.GetString("UserUuid")),
                reader.GetString(UserAccountTable.Columns.Username),
                reader.GetNullableString(UserAccountTable.Columns.DisplayName),
                reader.GetBoolean(reader.GetOrdinal(UserAccountTable.Columns.EmailVerified)),
                reader.GetBoolean(reader.GetOrdinal(UserAccountTable.Columns.IsActive)),
                reader.GetString(CompanyUserTable.Columns.CompanyRole),
                reader.GetString(CompanyUserTable.Columns.MembershipStatus),
                reader.GetUtcDateTime(UserAccountTable.Columns.CreatedAt)));
        }

        return users;
    }

    public async Task<int> CountActiveOwnersAsync(
        Guid companyUuid,
        CancellationToken cancellationToken)
    {
        var databaseTransaction = CurrentTransaction();
        var command = databaseTransaction.Connection.CreateCommand();
        command.Transaction = databaseTransaction.Transaction;
        command.CommandText =
            $"""
            SELECT COUNT(*)
            FROM {CompanyTable.Name} c
            INNER JOIN {CompanyUserTable.Name} cu
                ON cu.{CompanyUserTable.Columns.CompanyId} = c.{CompanyTable.Columns.CompanyId}
            INNER JOIN {UserAccountTable.Name} ua
                ON ua.{UserAccountTable.Columns.UserId} = cu.{CompanyUserTable.Columns.UserId}
            WHERE c.{CompanyTable.Columns.CompanyUuid} = UUID_TO_BIN(@CompanyUuid)
              AND c.{CompanyTable.Columns.IsDeleted} = 0
              AND ua.{UserAccountTable.Columns.IsDeleted} = 0
              AND ua.{UserAccountTable.Columns.IsActive} = 1
              AND cu.{CompanyUserTable.Columns.CompanyRole} = 'OWNER'
              AND cu.{CompanyUserTable.Columns.MembershipStatus} = 'active';
            """;
        command.Parameters.AddWithValue("@CompanyUuid", companyUuid.ToString());

        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task UpdateMembershipAsync(
        UpdateCompanyUserCommand command,
        CancellationToken cancellationToken)
    {
        var databaseTransaction = CurrentTransaction();
        var updateCommand = databaseTransaction.Connection.CreateCommand();
        updateCommand.Transaction = databaseTransaction.Transaction;
        updateCommand.CommandText =
            $"""
            UPDATE {CompanyUserTable.Name} cu
            INNER JOIN {CompanyTable.Name} c
                ON c.{CompanyTable.Columns.CompanyId} = cu.{CompanyUserTable.Columns.CompanyId}
            INNER JOIN {UserAccountTable.Name} ua
                ON ua.{UserAccountTable.Columns.UserId} = cu.{CompanyUserTable.Columns.UserId}
            SET
                cu.{CompanyUserTable.Columns.CompanyRole} = @CompanyRole,
                cu.{CompanyUserTable.Columns.MembershipStatus} = @MembershipStatus
            WHERE c.{CompanyTable.Columns.CompanyUuid} = UUID_TO_BIN(@CompanyUuid)
              AND ua.{UserAccountTable.Columns.UserUuid} = UUID_TO_BIN(@UserUuid)
              AND c.{CompanyTable.Columns.IsDeleted} = 0
              AND ua.{UserAccountTable.Columns.IsDeleted} = 0;
            """;
        updateCommand.Parameters.AddWithValue("@CompanyUuid", command.CompanyUuid.ToString());
        updateCommand.Parameters.AddWithValue("@UserUuid", command.UserUuid.ToString());
        updateCommand.Parameters.AddWithValue("@CompanyRole", command.CompanyRole);
        updateCommand.Parameters.AddWithValue("@MembershipStatus", command.MembershipStatus);

        if (await updateCommand.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new ServiceException(
                (int)HttpStatusCode.NotFound,
                "COMPANY_USER_NOT_FOUND",
                "Company user could not be found.");
        }
    }

    private MySqlDatabaseTransaction CurrentTransaction() =>
        _transactionAccessor.GetCurrent().AsMySql();
}

using MySqlConnector;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.CompanyUsers;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Domain.Interfaces.Repositories.CompanyUsers;
using RMS.Identity.Service.Infrastructure.Data;
using RMS.Identity.Service.Infrastructure.Persistence.Schema;
using System.Net;

namespace RMS.Identity.Service.Infrastructure.Persistence.CompanyUsers;

public sealed class CompanyUserMySqlRepository : ICompanyUserWriteRepository
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

    private MySqlDatabaseTransaction CurrentTransaction() =>
        _transactionAccessor.GetCurrent().AsMySql();
}

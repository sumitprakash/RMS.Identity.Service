using Dapper;
using RMS.Identity.Service.Domain.Entities;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Infrastructure.Persistence.Internal;

namespace RMS.Identity.Service.Infrastructure.Persistence;

internal sealed class CompanyRepository : RepositoryBase, ICompanyRepository
{
    public CompanyRepository(MySqlConnectionFactory connectionFactory, DbSessionAccessor sessionAccessor)
        : base(connectionFactory, sessionAccessor)
    {
    }

    public Task<Company?> GetByIdAsync(long companyId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                CompanyID,
                BIN_TO_UUID(CompanyUUID) AS CompanyUUID,
                CompanyCode,
                CompanyName,
                CompanyGSTIN,
                IsDeleted,
                CreatedAt,
                CreatedBy,
                UpdatedAt,
                UpdatedBy
            FROM Company
            WHERE CompanyID = @CompanyID
              AND IsDeleted = 0
            LIMIT 1;
            """;

        return WithConnectionAsync(
            (connection, transaction) => connection.QuerySingleOrDefaultAsync<Company>(
                new CommandDefinition(sql, new { CompanyID = companyId }, transaction, cancellationToken: cancellationToken)),
            cancellationToken);
    }

    public Task<Company?> GetByUuidAsync(Guid companyUuid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                CompanyID,
                BIN_TO_UUID(CompanyUUID) AS CompanyUUID,
                CompanyCode,
                CompanyName,
                CompanyGSTIN,
                IsDeleted,
                CreatedAt,
                CreatedBy,
                UpdatedAt,
                UpdatedBy
            FROM Company
            WHERE CompanyUUID = UUID_TO_BIN(@CompanyUUID)
              AND IsDeleted = 0
            LIMIT 1;
            """;

        return WithConnectionAsync(
            (connection, transaction) => connection.QuerySingleOrDefaultAsync<Company>(
                new CommandDefinition(sql, new { CompanyUUID = companyUuid }, transaction, cancellationToken: cancellationToken)),
            cancellationToken);
    }
}

using MySqlConnector;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.Companies;
using RMS.Identity.Service.Domain.Entities.Companies;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Companies;
using RMS.Identity.Service.Infrastructure.Data;
using RMS.Identity.Service.Infrastructure.Persistence.Schema;

namespace RMS.Identity.Service.Infrastructure.Persistence.Companies;

public sealed class CompanyMySqlRepository :
    ICompanyReadRepository,
    ICompanyWriteRepository
{
    private readonly IDatabaseTransactionAccessor _transactionAccessor;

    public CompanyMySqlRepository(IDatabaseTransactionAccessor transactionAccessor)
    {
        _transactionAccessor = transactionAccessor;
    }

    public async Task<bool> ExistsByGstinAsync(
        string gstin,
        CancellationToken cancellationToken)
    {
        var databaseTransaction = CurrentTransaction();
        var command = databaseTransaction.Connection.CreateCommand();
        command.Transaction = databaseTransaction.Transaction;
        command.CommandText =
            $"""
            SELECT 1
            FROM {CompanyTable.Name}
            WHERE {CompanyTable.Columns.CompanyGstin} = @CompanyGSTIN
              AND {CompanyTable.Columns.IsDeleted} = 0
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@CompanyGSTIN", gstin);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    public async Task<long> CreateAsync(
        CreateCompanyCommand command,
        CancellationToken cancellationToken)
    {
        var databaseTransaction = CurrentTransaction();
        var insertCommand = databaseTransaction.Connection.CreateCommand();
        insertCommand.Transaction = databaseTransaction.Transaction;
        insertCommand.CommandText =
            $"""
            INSERT INTO {CompanyTable.Name} (
                {CompanyTable.Columns.CompanyUuid},
                {CompanyTable.Columns.LegalName},
                {CompanyTable.Columns.TradeName},
                {CompanyTable.Columns.CompanyGstin},
                {CompanyTable.Columns.ContactEmailAddress},
                {CompanyTable.Columns.ContactPhoneNumber},
                {CompanyTable.Columns.AddressLine1},
                {CompanyTable.Columns.AddressLine2},
                {CompanyTable.Columns.City},
                {CompanyTable.Columns.State},
                {CompanyTable.Columns.PostalCode},
                {CompanyTable.Columns.Country},
                {CompanyTable.Columns.CompanyStatus},
                {CompanyTable.Columns.IsDeleted},
                {CompanyTable.Columns.CreatedAt})
            VALUES (
                UUID_TO_BIN(@CompanyUuid),
                @LegalName,
                @TradeName,
                @CompanyGSTIN,
                @ContactEmailAddress,
                @ContactPhoneNumber,
                @AddressLine1,
                @AddressLine2,
                @City,
                @State,
                @PostalCode,
                @Country,
                @CompanyStatus,
                0,
                UTC_TIMESTAMP());
            """;
        insertCommand.Parameters.AddWithValue("@CompanyUuid", command.CompanyUuid.ToString());
        insertCommand.Parameters.AddWithValue("@LegalName", command.LegalName);
        insertCommand.Parameters.AddWithValue("@TradeName", (object?)command.TradeName ?? DBNull.Value);
        insertCommand.Parameters.AddWithValue("@CompanyGSTIN", command.Gstin);
        insertCommand.Parameters.AddWithValue("@ContactEmailAddress", command.ContactEmailAddress);
        insertCommand.Parameters.AddWithValue("@ContactPhoneNumber", command.ContactPhoneNumber);
        insertCommand.Parameters.AddWithValue("@AddressLine1", command.AddressLine1);
        insertCommand.Parameters.AddWithValue("@AddressLine2", (object?)command.AddressLine2 ?? DBNull.Value);
        insertCommand.Parameters.AddWithValue("@City", command.City);
        insertCommand.Parameters.AddWithValue("@State", command.State);
        insertCommand.Parameters.AddWithValue("@PostalCode", command.PostalCode);
        insertCommand.Parameters.AddWithValue("@Country", command.Country);
        insertCommand.Parameters.AddWithValue("@CompanyStatus", command.Status);

        try
        {
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
            return insertCommand.LastInsertedId;
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            throw new ConflictException(ServiceErrorDefinitions.Companies.CompanyExists);
        }
    }

    public async Task UpdateAsync(
        UpdateCompanyCommand command,
        CancellationToken cancellationToken)
    {
        var databaseTransaction = CurrentTransaction();
        var updateCommand = databaseTransaction.Connection.CreateCommand();
        updateCommand.Transaction = databaseTransaction.Transaction;
        updateCommand.CommandText =
            $"""
            UPDATE {CompanyTable.Name}
            SET
                {CompanyTable.Columns.LegalName} = @LegalName,
                {CompanyTable.Columns.TradeName} = @TradeName,
                {CompanyTable.Columns.CompanyGstin} = @CompanyGSTIN,
                {CompanyTable.Columns.ContactEmailAddress} = @ContactEmailAddress,
                {CompanyTable.Columns.ContactPhoneNumber} = @ContactPhoneNumber,
                {CompanyTable.Columns.AddressLine1} = @AddressLine1,
                {CompanyTable.Columns.AddressLine2} = @AddressLine2,
                {CompanyTable.Columns.City} = @City,
                {CompanyTable.Columns.State} = @State,
                {CompanyTable.Columns.PostalCode} = @PostalCode,
                {CompanyTable.Columns.Country} = @Country
            WHERE {CompanyTable.Columns.CompanyUuid} = UUID_TO_BIN(@CompanyUuid)
              AND {CompanyTable.Columns.IsDeleted} = 0;
            """;
        updateCommand.Parameters.AddWithValue("@CompanyUuid", command.CompanyUuid.ToString());
        updateCommand.Parameters.AddWithValue("@LegalName", command.LegalName);
        updateCommand.Parameters.AddWithValue("@TradeName", (object?)command.TradeName ?? DBNull.Value);
        updateCommand.Parameters.AddWithValue("@CompanyGSTIN", command.Gstin);
        updateCommand.Parameters.AddWithValue("@ContactEmailAddress", command.ContactEmailAddress);
        updateCommand.Parameters.AddWithValue("@ContactPhoneNumber", command.ContactPhoneNumber);
        updateCommand.Parameters.AddWithValue("@AddressLine1", command.AddressLine1);
        updateCommand.Parameters.AddWithValue("@AddressLine2", (object?)command.AddressLine2 ?? DBNull.Value);
        updateCommand.Parameters.AddWithValue("@City", command.City);
        updateCommand.Parameters.AddWithValue("@State", command.State);
        updateCommand.Parameters.AddWithValue("@PostalCode", command.PostalCode);
        updateCommand.Parameters.AddWithValue("@Country", command.Country);

        try
        {
            await updateCommand.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            throw new ConflictException(ServiceErrorDefinitions.Companies.CompanyExists);
        }
    }

    public async Task UpdateStatusAsync(
        UpdateCompanyStatusCommand command,
        CancellationToken cancellationToken)
    {
        var databaseTransaction = CurrentTransaction();
        var updateCommand = databaseTransaction.Connection.CreateCommand();
        updateCommand.Transaction = databaseTransaction.Transaction;
        updateCommand.CommandText =
            $"""
            UPDATE {CompanyTable.Name}
            SET {CompanyTable.Columns.CompanyStatus} = @CompanyStatus
            WHERE {CompanyTable.Columns.CompanyUuid} = UUID_TO_BIN(@CompanyUuid)
              AND {CompanyTable.Columns.IsDeleted} = 0;
            """;
        updateCommand.Parameters.AddWithValue("@CompanyUuid", command.CompanyUuid.ToString());
        updateCommand.Parameters.AddWithValue("@CompanyStatus", command.Status);

        if (await updateCommand.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new ResourceNotFoundException(ServiceErrorDefinitions.Companies.CompanyNotFound);
        }
    }

    public async Task<Company> GetByIdAsync(
        long companyId,
        CancellationToken cancellationToken)
    {
        return await GetSingleAsync(
            $"""
            WHERE {CompanyTable.Columns.CompanyId} = @CompanyId
            """,
            command => command.Parameters.AddWithValue("@CompanyId", companyId),
            () => new InternalServerErrorException("Company could not be loaded."),
            cancellationToken);
    }

    public async Task<Company> GetByUuidAsync(
        Guid companyUuid,
        CancellationToken cancellationToken)
    {
        return await GetSingleAsync(
            $"""
            WHERE {CompanyTable.Columns.CompanyUuid} = UUID_TO_BIN(@CompanyUuid)
              AND {CompanyTable.Columns.IsDeleted} = 0
            """,
            command => command.Parameters.AddWithValue("@CompanyUuid", companyUuid.ToString()),
            () => new ResourceNotFoundException(ServiceErrorDefinitions.Companies.CompanyNotFound),
            cancellationToken);
    }

    private async Task<Company> GetSingleAsync(
        string whereClause,
        Action<MySqlCommand> configure,
        Func<ServiceException> notFound,
        CancellationToken cancellationToken)
    {
        var databaseTransaction = CurrentTransaction();
        var command = databaseTransaction.Connection.CreateCommand();
        command.Transaction = databaseTransaction.Transaction;
        command.CommandText =
            $"""
            SELECT
                {CompanyTable.Columns.CompanyId},
                BIN_TO_UUID({CompanyTable.Columns.CompanyUuid}) AS CompanyUuid,
                {CompanyTable.Columns.LegalName},
                {CompanyTable.Columns.TradeName},
                {CompanyTable.Columns.CompanyGstin},
                {CompanyTable.Columns.ContactEmailAddress},
                {CompanyTable.Columns.ContactPhoneNumber},
                {CompanyTable.Columns.AddressLine1},
                {CompanyTable.Columns.AddressLine2},
                {CompanyTable.Columns.City},
                {CompanyTable.Columns.State},
                {CompanyTable.Columns.PostalCode},
                {CompanyTable.Columns.Country},
                {CompanyTable.Columns.CompanyStatus},
                {CompanyTable.Columns.IsDeleted},
                {CompanyTable.Columns.CreatedAt}
            FROM {CompanyTable.Name}
            {whereClause}
            LIMIT 1;
            """;
        configure(command);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw notFound();
        }

        return new Company(
            reader.GetInt64(reader.GetOrdinal(CompanyTable.Columns.CompanyId)),
            Guid.Parse(reader.GetString("CompanyUuid")),
            reader.GetString(CompanyTable.Columns.LegalName),
            reader.GetNullableString(CompanyTable.Columns.TradeName),
            reader.GetString(CompanyTable.Columns.CompanyGstin),
            reader.GetString(CompanyTable.Columns.ContactEmailAddress),
            reader.GetString(CompanyTable.Columns.ContactPhoneNumber),
            reader.GetString(CompanyTable.Columns.AddressLine1),
            reader.GetNullableString(CompanyTable.Columns.AddressLine2),
            reader.GetString(CompanyTable.Columns.City),
            reader.GetString(CompanyTable.Columns.State),
            reader.GetString(CompanyTable.Columns.PostalCode),
            reader.GetString(CompanyTable.Columns.Country),
            reader.GetString(CompanyTable.Columns.CompanyStatus),
            reader.GetBoolean(reader.GetOrdinal(CompanyTable.Columns.IsDeleted)),
            reader.GetUtcDateTime(CompanyTable.Columns.CreatedAt));
    }

    private MySqlDatabaseTransaction CurrentTransaction() =>
        _transactionAccessor.GetCurrent().AsMySql();
}

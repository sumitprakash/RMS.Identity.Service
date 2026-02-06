using MySqlConnector;
using RMS.Identity.Service.Application.Repositories;
using RMS.Identity.Service.Domain.Entities;
using RMS.Identity.Service.Infrastructure.Data;
using RMS.Identity.Service.Infrastructure.Utils;
using System.Data.Common;

namespace RMS.Identity.Service.Infrastructure.Repositories
{
    public class CompanyRepository : ICompanyRepository
    {
        private readonly IDbConnectionFactory _dbFactory;
        public CompanyRepository(IDbConnectionFactory dbFactory) => _dbFactory = dbFactory;

        public async Task<Company?> GetByCompanyUuidAsync(Guid companyUuid)
        {
            using var conn = await _dbFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT CompanyID, CompanyUUID, CompanyCode, CompanyName, CompanyGSTIN, IsDeleted, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
FROM Company
WHERE CompanyUUID = @companyUuid AND IsDeleted = 0 LIMIT 1;";
            ((MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@companyUuid", GuidUtils.ToBytes(companyUuid), System.Data.DbType.Binary));

            using var reader = await ((DbCommand)cmd).ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            var c = new Company
            {
                CompanyID = reader.GetInt64(reader.GetOrdinal("CompanyID")),
                CompanyUUID = GuidUtils.FromBytes(reader[reader.GetOrdinal("CompanyUUID")]),
                CompanyCode = reader.GetString(reader.GetOrdinal("CompanyCode")),
                CompanyName = reader.GetString(reader.GetOrdinal("CompanyName")),
                CompanyGSTIN = reader.IsDBNull(reader.GetOrdinal("CompanyGSTIN")) ? null : reader.GetString(reader.GetOrdinal("CompanyGSTIN")),
                IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? null : reader.GetInt64(reader.GetOrdinal("CreatedBy")),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                UpdatedBy = reader.IsDBNull(reader.GetOrdinal("UpdatedBy")) ? null : reader.GetInt64(reader.GetOrdinal("UpdatedBy"))
            };
            return c;
        }

        public async Task<Company?> GetByIdAsync(long companyId)
        {
            using var conn = await _dbFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT CompanyID, CompanyUUID, CompanyCode, CompanyName, CompanyGSTIN, IsDeleted, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
FROM Company
WHERE CompanyID = @companyId AND IsDeleted = 0 LIMIT 1;";
            ((MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@companyId", companyId, System.Data.DbType.Int64));

            using var reader = await ((DbCommand)cmd).ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            var c = new Company
            {
                CompanyID = reader.GetInt64(reader.GetOrdinal("CompanyID")),
                CompanyUUID = GuidUtils.FromBytes(reader[reader.GetOrdinal("CompanyUUID")]),
                CompanyCode = reader.GetString(reader.GetOrdinal("CompanyCode")),
                CompanyName = reader.GetString(reader.GetOrdinal("CompanyName")),
                CompanyGSTIN = reader.IsDBNull(reader.GetOrdinal("CompanyGSTIN")) ? null : reader.GetString(reader.GetOrdinal("CompanyGSTIN")),
                IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? null : reader.GetInt64(reader.GetOrdinal("CreatedBy")),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                UpdatedBy = reader.IsDBNull(reader.GetOrdinal("UpdatedBy")) ? null : reader.GetInt64(reader.GetOrdinal("UpdatedBy"))
            };
            return c;
        }

        public async Task<long> CreateAsync(Company company)
        {
            using var conn = await _dbFactory.CreateOpenConnectionAsync();
            using var tx = await ((MySqlConnection)conn).BeginTransactionAsync();
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT INTO Company (CompanyUUID, CompanyCode, CompanyName, CompanyGSTIN, IsDeleted, CreatedAt, CreatedBy)
VALUES (@CompanyUUID, @CompanyCode, @CompanyName, @CompanyGSTIN, @IsDeleted, @CreatedAt, @CreatedBy);
SELECT LAST_INSERT_ID();";
                ((MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@CompanyUUID", GuidUtils.ToBytes(company.CompanyUUID), System.Data.DbType.Binary));
                ((MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@CompanyCode", company.CompanyCode));
                ((MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@CompanyName", company.CompanyName));
                ((MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@CompanyGSTIN", company.CompanyGSTIN ?? (object)DBNull.Value));
                ((MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@IsDeleted", company.IsDeleted, System.Data.DbType.Boolean));
                ((MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@CreatedAt", company.CreatedAt, System.Data.DbType.DateTime));
                ((MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@CreatedBy", company.CreatedBy ?? (object)DBNull.Value, System.Data.DbType.Int64));

                var result = await ((DbCommand)cmd).ExecuteScalarAsync();
                var id = Convert.ToInt64(result);
                await tx.CommitAsync();
                return id;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}

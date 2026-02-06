using RMS.Identity.Service.Domain.Entities;
using RMS.Identity.Service.Infrastructure.Data;
using RMS.Identity.Service.Infrastructure.Utils;
using System.Data.Common;

namespace RMS.Identity.Service.Infrastructure.Repositories.Implementation
{
    public class RoleRepository : IRoleRepository
    {
        private readonly IDbConnectionFactory _dbFactory;
        public RoleRepository(IDbConnectionFactory dbFactory) => _dbFactory = dbFactory;

        public async Task<Role?> GetByNameAsync(string name)
        {
            using var conn = await _dbFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT RoleID, RoleUUID, Name, Description, IsSystemRole, IsDeleted, CreatedAt FROM Role WHERE Name = @Name AND IsDeleted = 0 LIMIT 1;";
            ((MySqlConnector.MySqlParameterCollection)cmd.Parameters).Add(RMS.Identity.Service.Infrastructure.Utils.DbParameterFactory.Create("@Name", name));
            using var reader = await ((DbCommand)cmd).ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            return new Role
            {
                RoleID = reader.GetInt64(reader.GetOrdinal("RoleID")),
                RoleUUID = GuidUtils.FromBytes(reader[reader.GetOrdinal("RoleUUID")]),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                IsSystemRole = reader.GetBoolean(reader.GetOrdinal("IsSystemRole"))
            };
        }

        public async Task<long> CreateAsync(Role role)
        {
            using var conn = await _dbFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO Role (RoleUUID, Name, Description, IsSystemRole, IsDeleted, CreatedAt)
VALUES (@RoleUUID, @Name, @Description, @IsSystemRole, @IsDeleted, @CreatedAt);
SELECT LAST_INSERT_ID();";
            ((MySqlConnector.MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@RoleUUID", GuidUtils.ToBytes(role.RoleUUID), System.Data.DbType.Binary));
            ((MySqlConnector.MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@Name", role.Name));
            ((MySqlConnector.MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@Description", role.Description ?? (object)DBNull.Value));
            ((MySqlConnector.MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@IsSystemRole", role.IsSystemRole, System.Data.DbType.Boolean));
            ((MySqlConnector.MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@IsDeleted", role.IsDeleted, System.Data.DbType.Boolean));
            ((MySqlConnector.MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@CreatedAt", role.CreatedAt, System.Data.DbType.DateTime));
            var id = Convert.ToInt64(await ((DbCommand)cmd).ExecuteScalarAsync());
            return id;
        }

        public async Task<Role?> GetByIdAsync(long roleId)
        {
            using var conn = await _dbFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT RoleID, RoleUUID, Name, Description, IsSystemRole, IsDeleted, CreatedAt FROM Role WHERE RoleID = @RoleID LIMIT 1;";
            ((MySqlConnector.MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@RoleID", roleId, System.Data.DbType.Int64));
            using var reader = await ((DbCommand)cmd).ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            return new Role
            {
                RoleID = reader.GetInt64(reader.GetOrdinal("RoleID")),
                RoleUUID = GuidUtils.FromBytes(reader[reader.GetOrdinal("RoleUUID")]),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                IsSystemRole = reader.GetBoolean(reader.GetOrdinal("IsSystemRole"))
            };
        }
    }
}

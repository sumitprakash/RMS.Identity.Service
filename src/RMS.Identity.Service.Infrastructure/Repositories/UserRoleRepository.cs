using RMS.Identity.Service.Application.Repositories;
using RMS.Identity.Service.Domain.Entities;
using RMS.Identity.Service.Infrastructure.Data;
using RMS.Identity.Service.Infrastructure.Utils;
using System.Data.Common;

namespace RMS.Identity.Service.Infrastructure.Repositories
{
    public class UserRoleRepository : IUserRoleRepository
    {
        private readonly IDbConnectionFactory _dbFactory;
        public UserRoleRepository(IDbConnectionFactory dbFactory) => _dbFactory = dbFactory;

        public async Task AssignRoleAsync(long userId, long roleId, long? assignedBy)
        {
            using var conn = await _dbFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT IGNORE INTO UserRole (UserID, RoleID, AssignedAt, AssignedBy) VALUES (@UserID, @RoleID, NOW(), @AssignedBy);";
            ((MySqlConnector.MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@UserID", userId, System.Data.DbType.Int64));
            ((MySqlConnector.MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@RoleID", roleId, System.Data.DbType.Int64));
            ((MySqlConnector.MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@AssignedBy", assignedBy ?? (object)DBNull.Value, System.Data.DbType.Int64));
            await ((DbCommand)cmd).ExecuteNonQueryAsync();
        }

        public async Task RemoveRoleAsync(long userId, long roleId)
        {
            using var conn = await _dbFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"DELETE FROM UserRole WHERE UserID = @UserID AND RoleID = @RoleID;";
            ((MySqlConnector.MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@UserID", userId, System.Data.DbType.Int64));
            ((MySqlConnector.MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@RoleID", roleId, System.Data.DbType.Int64));
            await ((DbCommand)cmd).ExecuteNonQueryAsync();
        }

        public async Task<IEnumerable<Role>> GetRolesForUserAsync(long userId)
        {
            using var conn = await _dbFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT r.RoleID, r.RoleUUID, r.Name, r.Description, r.IsSystemRole, r.IsDeleted, r.CreatedAt
FROM Role r
JOIN UserRole ur ON ur.RoleID = r.RoleID
WHERE ur.UserID = @USERID AND r.IsDeleted = 0;";
            ((MySqlConnector.MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@USERID", userId, System.Data.DbType.Int64));
            var result = new List<Role>();
            using var reader = await ((DbCommand)cmd).ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new Role
                {
                    RoleID = reader.GetInt64(reader.GetOrdinal("RoleID")),
                    RoleUUID = GuidUtils.FromBytes(reader[reader.GetOrdinal("RoleUUID")]),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    IsSystemRole = reader.GetBoolean(reader.GetOrdinal("IsSystemRole"))
                });
            }
            return result;
        }
    }
}

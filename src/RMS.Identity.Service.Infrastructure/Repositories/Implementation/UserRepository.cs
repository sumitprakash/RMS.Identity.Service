using MySqlConnector;
using RMS.Identity.Service.Domain.Entities;
using RMS.Identity.Service.Infrastructure.Data;
using RMS.Identity.Service.Infrastructure.Utils;
using System.Data.Common;

namespace RMS.Identity.Service.Infrastructure.Repositories.Implementation
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnectionFactory _dbFactory;
        public UserRepository(IDbConnectionFactory dbFactory) => _dbFactory = dbFactory;

        public async Task<UserAccount?> GetByUsernameAsync(long companyId, string username)
        {
            using var conn = await _dbFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT UserID, UserUUID, CompanyID, Username, PasswordHash, DisplayName, IsActive, IsDeleted, CreatedAt, CreatedBy
FROM UserAccount
WHERE CompanyID = @CompanyID AND Username = @Username AND IsDeleted = 0
LIMIT 1;";
            ((MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@CompanyID", companyId, System.Data.DbType.Int64));
            ((MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@Username", username));

            using var reader = await ((DbCommand)cmd).ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            var user = new UserAccount
            {
                UserID = reader.GetInt64(reader.GetOrdinal("UserID")),
                UserUUID = GuidUtils.FromBytes(reader[reader.GetOrdinal("UserUUID")]),
                CompanyID = reader.GetInt64(reader.GetOrdinal("CompanyID")),
                Username = reader.GetString(reader.GetOrdinal("Username")),
                PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                DisplayName = reader.IsDBNull(reader.GetOrdinal("DisplayName")) ? null : reader.GetString(reader.GetOrdinal("DisplayName")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? null : (long?)reader.GetInt64(reader.GetOrdinal("CreatedBy")),
                Roles = new List<Role>()
            };

            return user;
        }

        public async Task<UserAccount?> GetByUserUuidAsync(Guid userUuid)
        {
            using var conn = await _dbFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT UserID, UserUUID, CompanyID, Username, PasswordHash, DisplayName, IsActive, IsDeleted, CreatedAt, CreatedBy
FROM UserAccount
WHERE UserUUID = @UserUUID AND IsDeleted = 0
LIMIT 1;";
            ((MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@UserUUID", GuidUtils.ToBytes(userUuid), System.Data.DbType.Binary));

            using var reader = await ((DbCommand)cmd).ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            var user = new UserAccount
            {
                UserID = reader.GetInt64(reader.GetOrdinal("UserID")),
                UserUUID = GuidUtils.FromBytes(reader[reader.GetOrdinal("UserUUID")]),
                CompanyID = reader.GetInt64(reader.GetOrdinal("CompanyID")),
                Username = reader.GetString(reader.GetOrdinal("Username")),
                PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                DisplayName = reader.IsDBNull(reader.GetOrdinal("DisplayName")) ? null : reader.GetString(reader.GetOrdinal("DisplayName")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? null : (long?)reader.GetInt64(reader.GetOrdinal("CreatedBy")),
                Roles = new List<Role>()
            };

            return user;
        }

        public async Task<UserAccount?> GetByIdAsync(long userId)
        {
            using var conn = await _dbFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT UserID, UserUUID, CompanyID, Username, PasswordHash, DisplayName, IsActive, IsDeleted, CreatedAt, CreatedBy
FROM UserAccount
WHERE UserID = @UserID AND IsDeleted = 0
LIMIT 1;";
            ((MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@UserID", userId, System.Data.DbType.Int64));

            using var reader = await ((DbCommand)cmd).ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            var user = new UserAccount
            {
                UserID = reader.GetInt64(reader.GetOrdinal("UserID")),
                UserUUID = GuidUtils.FromBytes(reader[reader.GetOrdinal("UserUUID")]),
                CompanyID = reader.GetInt64(reader.GetOrdinal("CompanyID")),
                Username = reader.GetString(reader.GetOrdinal("Username")),
                PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                DisplayName = reader.IsDBNull(reader.GetOrdinal("DisplayName")) ? null : reader.GetString(reader.GetOrdinal("DisplayName")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? null : (long?)reader.GetInt64(reader.GetOrdinal("CreatedBy")),
                Roles = new List<Role>()
            };

            return user;
        }

        public async Task<UserAccount?> GetUserWithRolesAsync(long companyId, string username)
        {
            using var conn = await _dbFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT
  u.UserID, u.UserUUID, u.CompanyID, u.Username, u.PasswordHash, u.DisplayName, u.IsActive, u.IsDeleted, u.CreatedAt, u.CreatedBy,
  r.RoleID, r.RoleUUID, r.Name AS RoleName, r.Description AS RoleDescription, r.IsSystemRole
FROM UserAccount u
LEFT JOIN UserRole ur ON ur.UserID = u.UserID
LEFT JOIN Role r ON r.RoleID = ur.RoleID
WHERE u.CompanyID = @CompanyID AND u.Username = @Username AND u.IsDeleted = 0;";

            ((MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@CompanyID", companyId, System.Data.DbType.Int64));
            ((MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@Username", username));

            UserAccount? user = null;

            using var reader = await ((DbCommand)cmd).ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (user == null)
                {
                    user = new UserAccount
                    {
                        UserID = reader.GetInt64(reader.GetOrdinal("UserID")),
                        UserUUID = GuidUtils.FromBytes(reader[reader.GetOrdinal("UserUUID")]),
                        CompanyID = reader.GetInt64(reader.GetOrdinal("CompanyID")),
                        Username = reader.GetString(reader.GetOrdinal("Username")),
                        PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                        DisplayName = reader.IsDBNull(reader.GetOrdinal("DisplayName")) ? null : reader.GetString(reader.GetOrdinal("DisplayName")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? null : (long?)reader.GetInt64(reader.GetOrdinal("CreatedBy")),
                        Roles = new List<Role>()
                    };
                }

                if (!reader.IsDBNull(reader.GetOrdinal("RoleID")))
                {
                    var role = new Role
                    {
                        RoleID = reader.GetInt64(reader.GetOrdinal("RoleID")),
                        RoleUUID = GuidUtils.FromBytes(reader[reader.GetOrdinal("RoleUUID")]),
                        Name = reader.GetString(reader.GetOrdinal("RoleName")),
                        Description = reader.IsDBNull(reader.GetOrdinal("RoleDescription")) ? null : reader.GetString(reader.GetOrdinal("RoleDescription")),
                        IsSystemRole = reader.GetBoolean(reader.GetOrdinal("IsSystemRole"))
                    };
                    if (!user.Roles.Any(r => r.RoleID == role.RoleID))
                        user.Roles.Add(role);
                }
            }

            return user;
        }

        public async Task<long> CreateAsync(UserAccount user)
        {
            using var conn = await _dbFactory.CreateOpenConnectionAsync();
            using var tx = await ((MySqlConnector.MySqlConnection)conn).BeginTransactionAsync();
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT INTO UserAccount (UserUUID, CompanyID, Username, PasswordHash, DisplayName, IsActive, IsDeleted, CreatedAt, CreatedBy)
VALUES (@UserUUID, @CompanyID, @Username, @PasswordHash, @DisplayName, @IsActive, @IsDeleted, @CreatedAt, @CreatedBy);
SELECT LAST_INSERT_ID();";
                ((MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@UserUUID", GuidUtils.ToBytes(user.UserUUID), System.Data.DbType.Binary));
                ((MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@CompanyID", user.CompanyID, System.Data.DbType.Int64));
                ((MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@Username", user.Username));
                ((MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@PasswordHash", user.PasswordHash));
                ((MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@DisplayName", user.DisplayName ?? (object)DBNull.Value));
                ((MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@IsActive", user.IsActive, System.Data.DbType.Boolean));
                ((MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@IsDeleted", user.IsDeleted, System.Data.DbType.Boolean));
                ((MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@CreatedAt", user.CreatedAt, System.Data.DbType.DateTime));
                ((MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@CreatedBy", user.CreatedBy ?? (object)DBNull.Value, System.Data.DbType.Int64));

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

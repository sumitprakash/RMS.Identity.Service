using RMS.Identity.Service.Domain.Entities;
using RMS.Identity.Service.Infrastructure.Data;
using RMS.Identity.Service.Infrastructure.Utils;
using System.Data.Common;

namespace RMS.Identity.Service.Infrastructure.Repositories.Implementation
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly IDbConnectionFactory _dbFactory;
        public RefreshTokenRepository(IDbConnectionFactory dbFactory) => _dbFactory = dbFactory;

        public async Task SaveAsync(RefreshToken rt)
        {
            using var conn = await _dbFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO RefreshToken (UserID, TokenHash, ExpiresAt, CreatedAt, RevokedAt, ReplacedByTokenHash)
VALUES (@UserID, @TokenHash, @ExpiresAt, @CreatedAt, @RevokedAt, @ReplacedByTokenHash);
SELECT LAST_INSERT_ID();";
            ((MySqlConnector.MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@UserID", rt.UserID, System.Data.DbType.Int64));
            ((MySqlConnector.MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@TokenHash", rt.TokenHash));
            ((MySqlConnector.MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@ExpiresAt", rt.ExpiresAt, System.Data.DbType.DateTime));
            ((MySqlConnector.MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@CreatedAt", rt.CreatedAt, System.Data.DbType.DateTime));
            ((MySqlConnector.MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@RevokedAt", rt.RevokedAt ?? (object)DBNull.Value, System.Data.DbType.DateTime));
            ((MySqlConnector.MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@ReplacedByTokenHash", rt.ReplacedByTokenHash ?? (object)DBNull.Value));
            var id = Convert.ToInt64(await ((DbCommand)cmd).ExecuteScalarAsync());
            rt.RefreshTokenID = id;
        }

        public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
        {
            using var conn = await _dbFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT RefreshTokenID, UserID, TokenHash, ExpiresAt, CreatedAt, RevokedAt, ReplacedByTokenHash FROM RefreshToken WHERE TokenHash = @TokenHash LIMIT 1;";
            ((MySqlConnector.MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@TokenHash", tokenHash));
            using var reader = await ((DbCommand)cmd).ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            return new RefreshToken
            {
                RefreshTokenID = reader.GetInt64(reader.GetOrdinal("RefreshTokenID")),
                UserID = reader.GetInt64(reader.GetOrdinal("UserID")),
                TokenHash = reader.GetString(reader.GetOrdinal("TokenHash")),
                ExpiresAt = reader.GetDateTime(reader.GetOrdinal("ExpiresAt")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                RevokedAt = reader.IsDBNull(reader.GetOrdinal("RevokedAt")) ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("RevokedAt")),
                ReplacedByTokenHash = reader.IsDBNull(reader.GetOrdinal("ReplacedByTokenHash")) ? null : reader.GetString(reader.GetOrdinal("ReplacedByTokenHash"))
            };
        }

        public async Task RevokeAsync(long refreshTokenId, string? replacedByTokenHash = null)
        {
            using var conn = await _dbFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE RefreshToken SET RevokedAt = NOW(), ReplacedByTokenHash = @ReplacedBy WHERE RefreshTokenID = @Id";
            ((MySqlConnector.MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@ReplacedBy", replacedByTokenHash ?? (object)DBNull.Value));
            ((MySqlConnector.MySqlParameterCollection)cmd.Parameters).Add(DbParameterFactory.Create("@Id", refreshTokenId, System.Data.DbType.Int64));
            await ((DbCommand)cmd).ExecuteNonQueryAsync();
        }
    }
}

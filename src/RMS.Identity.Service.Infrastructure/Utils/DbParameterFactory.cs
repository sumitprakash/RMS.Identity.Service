using MySqlConnector;
using System.Data;

namespace RMS.Identity.Service.Infrastructure.Utils
{
    public static class DbParameterFactory
    {
        public static MySqlParameter Create(string name, object? value, DbType? dbType = null)
        {
            var p = new MySqlParameter(name, value ?? DBNull.Value);
            if (dbType.HasValue) p.DbType = dbType.Value;
            return p;
        }
    }
}

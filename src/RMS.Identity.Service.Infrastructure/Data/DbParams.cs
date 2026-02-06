using MySqlConnector;
using System.Data;

namespace RMS.Identity.Service.Infrastructure.Data
{
    public sealed class DbParams
    {
        private readonly List<MySqlParameter> _list = new();

        public DbParams Add(string name, object? value, DbType? dbType = null)
        {
            var p = new MySqlParameter(name, value ?? DBNull.Value);
            if (dbType.HasValue) p.DbType = dbType.Value;
            _list.Add(p);
            return this;
        }

        internal void ApplyTo(IDbCommand cmd)
        {
            foreach (var p in _list) cmd.Parameters.Add(p);
        }

        public static DbParams None => new DbParams();
    }
}
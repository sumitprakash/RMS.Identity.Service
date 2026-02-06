using System.Data;
using MySqlConnector;

namespace RMS.Identity.Service.Infrastructure.Data
{
    public sealed class DbSession : IDisposable
    {
        public IDbConnection Connection { get; }
        public IDbTransaction? Transaction { get; private set; }

        public DbSession(IDbConnectionFactory dbConnectionFactory)
        {
            Connection = dbConnectionFactory.CreateOpenConnectionAsync().GetAwaiter().GetResult();
        }

        public void BeginTransaction()
            => Transaction = Connection.BeginTransaction();

        public void Commit()
        {
            Transaction?.Commit();
            Transaction = null;
        }

        public void Rollback()
        {
            Transaction?.Rollback();
            Transaction = null;
        }

        public void Dispose()
        {
            Transaction?.Dispose();
            Connection.Dispose();
        }
    }
}
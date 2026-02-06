using System.Data;
using System.Data.Common;

namespace RMS.Identity.Service.Infrastructure.Data
{
    public sealed class DbExecutor
    {
        private readonly IUnitOfWork _uow;
        public DbExecutor(IUnitOfWork uow) => _uow = uow;

        private IDbCommand CreateCommand(string sql, DbParams? parameters)
        {
            var cmd = _uow.Connection.CreateCommand();
            cmd.CommandText = sql;
            if (_uow.IsInTransaction) cmd.Transaction = _uow.Transaction;
            parameters?.ApplyTo(cmd);
            return cmd;
        }

        public async Task<int> ExecuteAsync(string sql, DbParams? parameters = null)
        {
            using var cmd = CreateCommand(sql, parameters);
            return await ((DbCommand)cmd).ExecuteNonQueryAsync();
        }

        public async Task<T?> QuerySingleAsync<T>(string sql, Func<IDataReader, T> map, DbParams? parameters = null)
        {
            using var cmd = CreateCommand(sql, parameters);
            using var reader = await ((DbCommand)cmd).ExecuteReaderAsync();
            return await reader.ReadAsync() ? map(reader) : default;
        }

        public async Task<List<T>> QueryAsync<T>(string sql, Func<IDataReader, T> map, DbParams? parameters = null)
        {
            using var cmd = CreateCommand(sql, parameters);
            using var reader = await ((DbCommand)cmd).ExecuteReaderAsync();
            var list = new List<T>();
            while (await reader.ReadAsync()) list.Add(map(reader));
            return list;
        }
    }
}
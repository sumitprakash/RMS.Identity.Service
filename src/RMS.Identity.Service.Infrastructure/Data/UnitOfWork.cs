using System;
using System.Data;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace RMS.Identity.Service.Infrastructure.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly MySqlConnection _conn;
        private MySqlTransaction? _tx;

        public UnitOfWork(IConfiguration configuration)
        {
            var cs = configuration.GetConnectionString("Default")
                     ?? throw new InvalidOperationException("Missing ConnectionStrings:Default");
            _conn = new MySqlConnection(cs);
            _conn.Open();
        }

        public IDbConnection Connection => _conn;

        public IDbTransaction Transaction => _tx ?? throw new InvalidOperationException("Transaction not started");

        public bool IsInTransaction => _tx != null;

        public void Begin(IsolationLevel? isolationLevel = null)
        {
            if (_tx != null) throw new InvalidOperationException("Transaction already started");
            _tx = isolationLevel.HasValue ? _conn.BeginTransaction(isolationLevel.Value) : _conn.BeginTransaction();
        }

        public void Commit()
        {
            if (_tx == null) throw new InvalidOperationException("Transaction not started");
            _tx.Commit();
            _tx.Dispose();
            _tx = null;
        }

        public void Rollback()
        {
            if (_tx == null) return;
            try { _tx.Rollback(); }
            finally { _tx.Dispose(); _tx = null; }
        }

        public void Dispose()
        {
            try { _tx?.Dispose(); } catch { /* ignore */ }
            _conn.Dispose();
        }
    }
}
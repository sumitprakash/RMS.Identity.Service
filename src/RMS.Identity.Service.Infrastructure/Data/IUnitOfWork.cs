using System.Data;

namespace RMS.Identity.Service.Infrastructure.Data
{
    public interface IUnitOfWork : IDisposable
    {
        IDbConnection Connection { get; }
        IDbTransaction Transaction { get; } // throws if not started
        bool IsInTransaction { get; }

        void Begin(System.Data.IsolationLevel? isolationLevel = null);
        void Commit();
        void Rollback();
    }
}
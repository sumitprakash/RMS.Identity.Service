using RMS.Identity.Service.Domain.Interfaces.Persistence;

namespace RMS.Identity.Service.Infrastructure.Data;

internal static class MySqlDatabaseTransactionExtensions
{
    public static MySqlDatabaseTransaction AsMySql(this IDatabaseTransaction transaction)
    {
        return transaction as MySqlDatabaseTransaction
            ?? throw new InvalidOperationException("The database transaction is not backed by MySQL.");
    }
}

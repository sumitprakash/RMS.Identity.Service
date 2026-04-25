namespace RMS.Identity.Service.Domain.Interfaces.Persistence;

public interface IDatabaseTransactionExecutor
{
    Task<TResult> ExecuteAsync<TResult>(
        Func<IDatabaseTransaction, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken);
}

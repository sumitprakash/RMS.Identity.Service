namespace RMS.Identity.Service.Domain.Interfaces.Persistence;

public interface IDatabaseTransactionExecutor
{
    Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken);
}

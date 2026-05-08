using RMS.Identity.Service.Domain.Contracts.Idempotency;
using RMS.Identity.Service.Domain.Interfaces.Idempotency;
using RMS.Identity.Service.Domain.Interfaces.Persistence;

namespace RMS.Identity.Service.Api.Shared.Idempotency;

internal sealed class IdempotencyTransactionPipeline : IIdempotencyTransactionPipeline
{
    private readonly IDatabaseTransactionExecutor _transactionExecutor;
    private readonly IDatabaseTransactionAccessor _transactionAccessor;
    private readonly IIdempotencyService _idempotencyService;
    private readonly IIdempotencyResponseCapture _responseCapture;

    public IdempotencyTransactionPipeline(
        IDatabaseTransactionExecutor transactionExecutor,
        IDatabaseTransactionAccessor transactionAccessor,
        IIdempotencyService idempotencyService,
        IIdempotencyResponseCapture responseCapture)
    {
        _transactionExecutor = transactionExecutor;
        _transactionAccessor = transactionAccessor;
        _idempotencyService = idempotencyService;
        _responseCapture = responseCapture;
    }

    public Task<IdempotencyMiddlewareResponse> ExecuteAsync(
        HttpContext context,
        IdempotencyRequest idempotencyRequest,
        RequestDelegate next,
        CancellationToken cancellationToken) =>
        _transactionExecutor.ExecuteAsync(
            async (transaction, ct) =>
            {
                var previousTransaction = _transactionAccessor.Current;
                _transactionAccessor.Current = transaction;

                try
                {
                    var storedResponse = await _idempotencyService.ReserveAsync(transaction, idempotencyRequest, ct);
                    if (storedResponse is not null)
                    {
                        return IdempotencyMiddlewareResponse.FromStored(storedResponse);
                    }

                    var response = await _responseCapture.CaptureAsync(context, next, ct);

                    await _idempotencyService.StoreResponseAsync(
                        transaction,
                        idempotencyRequest.Key,
                        response.StatusCode,
                        response.Body,
                        ct);

                    return response;
                }
                finally
                {
                    _transactionAccessor.Current = previousTransaction;
                }
            },
            cancellationToken);
}

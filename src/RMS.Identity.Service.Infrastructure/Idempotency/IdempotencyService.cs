using System.Net;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.Idempotency;
using RMS.Identity.Service.Domain.Interfaces.Idempotency;
using RMS.Identity.Service.Domain.Interfaces.Persistence;

namespace RMS.Identity.Service.Infrastructure.Idempotency;

public sealed class IdempotencyService : IIdempotencyService
{
    private readonly IIdempotencyRepository _repository;

    public IdempotencyService(IIdempotencyRepository repository)
    {
        _repository = repository;
    }

    public async Task<IdempotencyStoredResponse?> ReserveAsync(
        IDatabaseTransaction transaction,
        IdempotencyRequest request,
        CancellationToken cancellationToken)
    {
        var existingRecord = await _repository.GetAsync(transaction, request.Key, lockForUpdate: false, cancellationToken);
        if (existingRecord is not null)
        {
            return ReadStoredResponse(existingRecord, request);
        }

        if (await _repository.TryCreateAsync(transaction, request, cancellationToken))
        {
            return null;
        }

        var collidedRecord = await _repository.GetAsync(transaction, request.Key, lockForUpdate: true, cancellationToken)
            ?? throw InProgress();

        return ReadStoredResponse(collidedRecord, request);
    }

    public Task StoreResponseAsync(
        IDatabaseTransaction transaction,
        string key,
        int responseCode,
        string responseBody,
        CancellationToken cancellationToken)
    {
        return _repository.StoreResponseAsync(
            transaction,
            key,
            responseCode,
            string.IsNullOrWhiteSpace(responseBody) ? "null" : responseBody,
            cancellationToken);
    }

    private static IdempotencyStoredResponse ReadStoredResponse(IdempotencyRecord record, IdempotencyRequest request)
    {
        if (!string.Equals(record.Method, request.Method, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(record.Route, request.Route, StringComparison.Ordinal))
        {
            throw Conflict("Idempotency key was already used for a different request.");
        }

        if (!string.Equals(record.RequestHash, request.RequestHash, StringComparison.Ordinal))
        {
            throw Conflict("Idempotency key payload does not match the original request.");
        }

        if (record.ResponseCode is null || string.IsNullOrWhiteSpace(record.ResponseBody))
        {
            throw InProgress();
        }

        return new IdempotencyStoredResponse(record.ResponseCode.Value, record.ResponseBody);
    }

    private static ServiceException Conflict(string message) =>
        new((int)HttpStatusCode.Conflict, "IDEMPOTENCY_KEY_REUSED", message);

    private static ServiceException InProgress() =>
        new((int)HttpStatusCode.Conflict, "IDEMPOTENCY_IN_PROGRESS", "Idempotent request is already in progress.");
}

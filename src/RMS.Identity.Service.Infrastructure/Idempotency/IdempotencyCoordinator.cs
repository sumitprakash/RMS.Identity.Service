using System.Net;
using System.Text.Json;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.Idempotency;
using RMS.Identity.Service.Domain.Interfaces.Idempotency;
using RMS.Identity.Service.Domain.Interfaces.Persistence;

namespace RMS.Identity.Service.Infrastructure.Idempotency;

public sealed class IdempotencyCoordinator : IIdempotencyCoordinator
{
    private readonly IIdempotencyRepository _repository;

    public IdempotencyCoordinator(IIdempotencyRepository repository)
    {
        _repository = repository;
    }

    public async Task<IdempotencyReservationResult<TResponse>> ReserveAsync<TResponse>(
        IDatabaseTransaction transaction,
        IdempotencyRequest request,
        CancellationToken cancellationToken)
    {
        var existingRecord = await _repository.GetAsync(transaction, request.Key, lockForUpdate: false, cancellationToken);
        if (existingRecord is not null)
        {
            return new IdempotencyReservationResult<TResponse>(false, ReadStoredResponse<TResponse>(existingRecord, request));
        }

        if (await _repository.TryCreateAsync(transaction, request, cancellationToken))
        {
            return new IdempotencyReservationResult<TResponse>(true, default);
        }

        var collidedRecord = await _repository.GetAsync(transaction, request.Key, lockForUpdate: true, cancellationToken)
            ?? throw InProgress();

        return new IdempotencyReservationResult<TResponse>(false, ReadStoredResponse<TResponse>(collidedRecord, request));
    }

    public Task StoreResponseAsync<TResponse>(
        IDatabaseTransaction transaction,
        string key,
        int responseCode,
        TResponse response,
        CancellationToken cancellationToken)
    {
        return _repository.StoreResponseAsync(
            transaction,
            key,
            responseCode,
            JsonSerializer.Serialize(response),
            cancellationToken);
    }

    private static TResponse ReadStoredResponse<TResponse>(IdempotencyRecord record, IdempotencyRequest request)
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

        return JsonSerializer.Deserialize<TResponse>(record.ResponseBody)
            ?? throw new ServiceException(
                (int)HttpStatusCode.InternalServerError,
                "IDEMPOTENCY_RESPONSE_INVALID",
                "Stored idempotent response is invalid.");
    }

    private static ServiceException Conflict(string message) =>
        new((int)HttpStatusCode.Conflict, "IDEMPOTENCY_KEY_REUSED", message);

    private static ServiceException InProgress() =>
        new((int)HttpStatusCode.Conflict, "IDEMPOTENCY_IN_PROGRESS", "Idempotent request is already in progress.");
}

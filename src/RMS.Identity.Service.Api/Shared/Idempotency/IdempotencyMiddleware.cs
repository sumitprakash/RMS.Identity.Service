using System.Net;
using System.Text;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.Idempotency;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Idempotency;

namespace RMS.Identity.Service.Api.Shared.Idempotency;

public sealed class IdempotencyMiddleware
{
    private static readonly HashSet<string> MethodsRequiringIdempotency = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Patch,
        HttpMethods.Delete
    };

    private readonly RequestDelegate _next;

    public IdempotencyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IIdempotencyRequestFactory requestFactory,
        IDatabaseTransactionExecutor transactionExecutor,
        IDatabaseTransactionAccessor transactionAccessor,
        IIdempotencyRepository idempotencyRepository)
    {
        if (!RequiresIdempotency(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var idempotencyRequest = await requestFactory.CreateAsync(context.Request, context.RequestAborted);
        var middlewareResponse = await ExecuteIdempotentRequestAsync(
            context,
            idempotencyRequest,
            transactionExecutor,
            transactionAccessor,
            idempotencyRepository);

        await WriteResponseAsync(context.Response, middlewareResponse, context.RequestAborted);
    }

    private static bool RequiresIdempotency(string method) =>
        MethodsRequiringIdempotency.Contains(method);

    private async Task<IdempotencyResponse> ExecuteIdempotentRequestAsync(
        HttpContext context,
        IdempotencyRequest idempotencyRequest,
        IDatabaseTransactionExecutor transactionExecutor,
        IDatabaseTransactionAccessor transactionAccessor,
        IIdempotencyRepository idempotencyRepository)
    {
        return await transactionExecutor.ExecuteAsync(
            async (transaction, cancellationToken) =>
            {
                var previousTransaction = transactionAccessor.Current;
                transactionAccessor.Current = transaction;

                try
                {
                    var existingResponse = await ReadExistingResponseAsync(
                        idempotencyRepository,
                        transaction,
                        idempotencyRequest,
                        cancellationToken);

                    if (existingResponse is not null)
                    {
                        return existingResponse;
                    }

                    if (!await idempotencyRepository.TryCreateAsync(transaction, idempotencyRequest, cancellationToken))
                    {
                        var collidedRecord = await idempotencyRepository.GetAsync(
                            transaction,
                            idempotencyRequest.Key,
                            lockForUpdate: true,
                            cancellationToken)
                            ?? throw InProgress();

                        return ReadExistingResponse(collidedRecord, idempotencyRequest);
                    }

                    var response = await CaptureResponseAsync(context, _next, cancellationToken);

                    await idempotencyRepository.StoreResponseAsync(
                        transaction,
                        idempotencyRequest.Key,
                        response.StatusCode,
                        string.IsNullOrWhiteSpace(response.Body) ? "null" : response.Body,
                        cancellationToken);

                    return response;
                }
                finally
                {
                    transactionAccessor.Current = previousTransaction;
                }
            },
            context.RequestAborted);
    }

    private static async Task<IdempotencyResponse?> ReadExistingResponseAsync(
        IIdempotencyRepository idempotencyRepository,
        IDatabaseTransaction transaction,
        IdempotencyRequest request,
        CancellationToken cancellationToken)
    {
        var record = await idempotencyRepository.GetAsync(
            transaction,
            request.Key,
            lockForUpdate: false,
            cancellationToken);

        return record is null ? null : ReadExistingResponse(record, request);
    }

    private static IdempotencyResponse ReadExistingResponse(
        IdempotencyRecord record,
        IdempotencyRequest request)
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

        return new IdempotencyResponse(record.ResponseCode.Value, "application/json", record.ResponseBody);
    }

    private static async Task<IdempotencyResponse> CaptureResponseAsync(
        HttpContext context,
        RequestDelegate next,
        CancellationToken cancellationToken)
    {
        var originalBody = context.Response.Body;
        await using var capturedBody = new MemoryStream();
        context.Response.Body = capturedBody;

        try
        {
            await next(context);
        }
        finally
        {
            context.Response.Body = originalBody;
        }

        capturedBody.Position = 0;
        using var reader = new StreamReader(capturedBody, Encoding.UTF8);
        var responseBody = await reader.ReadToEndAsync(cancellationToken);

        return new IdempotencyResponse(
            context.Response.StatusCode,
            context.Response.ContentType,
            responseBody);
    }

    private static async Task WriteResponseAsync(
        HttpResponse response,
        IdempotencyResponse idempotencyResponse,
        CancellationToken cancellationToken)
    {
        response.StatusCode = idempotencyResponse.StatusCode;

        if (!string.IsNullOrWhiteSpace(idempotencyResponse.ContentType))
        {
            response.ContentType = idempotencyResponse.ContentType;
        }

        await response.WriteAsync(idempotencyResponse.Body, cancellationToken);
    }

    private static ServiceException Conflict(string message) =>
        new((int)HttpStatusCode.Conflict, "IDEMPOTENCY_KEY_REUSED", message);

    private static ServiceException InProgress() =>
        new((int)HttpStatusCode.Conflict, "IDEMPOTENCY_IN_PROGRESS", "Idempotent request is already in progress.");

    private sealed record IdempotencyResponse(
        int StatusCode,
        string? ContentType,
        string Body);
}

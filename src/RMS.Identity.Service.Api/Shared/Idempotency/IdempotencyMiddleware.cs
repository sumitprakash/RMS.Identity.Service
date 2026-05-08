using System.Text;
using System.Text.Json;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.Idempotency;
using RMS.Identity.Service.Domain.Interfaces.Idempotency;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Domain.Interfaces.Security;

namespace RMS.Identity.Service.Api.Shared.Idempotency;

public sealed class IdempotencyMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
        IDatabaseTransactionExecutor transactionExecutor,
        IDatabaseTransactionAccessor transactionAccessor,
        IIdempotencyService idempotencyService,
        ITextHasher textHasher)
    {
        if (!MethodsRequiringIdempotency.Contains(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = context.Request.Headers[IdempotencyHttpHeaders.HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ServiceException(400, "VALIDATION_ERROR", "Idempotency-Key is required.");
        }

        if (!Guid.TryParse(idempotencyKey, out var parsedIdempotencyKey) || parsedIdempotencyKey == Guid.Empty)
        {
            throw new ServiceException(400, "VALIDATION_ERROR", "Idempotency-Key must be a valid UUID.");
        }

        var requestBody = await ReadRequestBodyAsync(context.Request, context.RequestAborted);
        var requestHash = textHasher.Hash(JsonSerializer.Serialize(new
        {
            path = context.Request.Path.Value ?? string.Empty,
            queryString = context.Request.QueryString.Value ?? string.Empty,
            body = requestBody
        }, JsonOptions));
        var idempotencyRequest = new IdempotencyRequest(
            parsedIdempotencyKey.ToString(),
            context.Request.Method,
            context.Request.Path.Value ?? string.Empty,
            requestHash);

        var middlewareResponse = await transactionExecutor.ExecuteAsync(
            async (transaction, ct) =>
            {
                var previousTransaction = transactionAccessor.Current;
                transactionAccessor.Current = transaction;

                try
                {
                    var storedResponse = await idempotencyService.ReserveAsync(transaction, idempotencyRequest, ct);
                    if (storedResponse is not null)
                    {
                        return new MiddlewareResponse(storedResponse.StatusCode, "application/json", storedResponse.ResponseBody);
                    }

                    var originalBody = context.Response.Body;
                    await using var capturedBody = new MemoryStream();
                    context.Response.Body = capturedBody;

                    try
                    {
                        await _next(context);
                    }
                    finally
                    {
                        context.Response.Body = originalBody;
                    }

                    capturedBody.Position = 0;
                    using var reader = new StreamReader(capturedBody, Encoding.UTF8);
                    var responseBody = await reader.ReadToEndAsync(ct);

                    await idempotencyService.StoreResponseAsync(
                        transaction,
                        idempotencyRequest.Key,
                        context.Response.StatusCode,
                        responseBody,
                        ct);

                    return new MiddlewareResponse(
                        context.Response.StatusCode,
                        context.Response.ContentType,
                        responseBody);
                }
                finally
                {
                    transactionAccessor.Current = previousTransaction;
                }
            },
            context.RequestAborted);

        context.Response.StatusCode = middlewareResponse.StatusCode;
        if (!string.IsNullOrWhiteSpace(middlewareResponse.ContentType))
        {
            context.Response.ContentType = middlewareResponse.ContentType;
        }

        await context.Response.WriteAsync(middlewareResponse.Body, context.RequestAborted);
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        request.EnableBuffering();

        using var reader = new StreamReader(
            request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024,
            leaveOpen: true);

        var body = await reader.ReadToEndAsync(cancellationToken);
        request.Body.Position = 0;
        return body;
    }

    private sealed record MiddlewareResponse(
        int StatusCode,
        string? ContentType,
        string Body);
}

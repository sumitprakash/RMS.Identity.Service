using System.Text;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.Idempotency;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Idempotency;
using RMS.Identity.Service.Domain.Interfaces.Security;

namespace RMS.Identity.Service.Api.Shared.Idempotency;

public sealed class IdempotencyService : IIdempotencyService
{
    private readonly ITextHasher _textHasher;
    private readonly IDatabaseTransactionExecutor _transactionExecutor;
    private readonly IIdempotencyReadRepository _idempotencyReadRepository;
    private readonly IIdempotencyWriteRepository _idempotencyWriteRepository;

    public IdempotencyService(
        ITextHasher textHasher,
        IDatabaseTransactionExecutor transactionExecutor,
        IIdempotencyReadRepository idempotencyReadRepository,
        IIdempotencyWriteRepository idempotencyWriteRepository)
    {
        _textHasher = textHasher;
        _transactionExecutor = transactionExecutor;
        _idempotencyReadRepository = idempotencyReadRepository;
        _idempotencyWriteRepository = idempotencyWriteRepository;
    }

    public async Task ExecuteAsync(
        HttpContext context,
        RequestDelegate next,
        CancellationToken cancellationToken)
    {
        var idempotencyRequest = await IdempotencyRequestFactory.CreateAsync(
            context.Request,
            _textHasher,
            cancellationToken);

        var response = await ExecuteIdempotentRequestAsync(context, idempotencyRequest, next);

        await WriteResponseAsync(context.Response, response, cancellationToken);
    }

    private async Task<IdempotencyResponse> ExecuteIdempotentRequestAsync(
        HttpContext context,
        IdempotencyRequest idempotencyRequest,
        RequestDelegate next)
    {
        return await _transactionExecutor.ExecuteAsync(
            async cancellationToken =>
            {
                var existingResponse = await ReadExistingResponseAsync(
                    idempotencyRequest,
                    cancellationToken);

                if (existingResponse is not null)
                {
                    return existingResponse;
                }

                if (!await _idempotencyWriteRepository.TryCreateAsync(idempotencyRequest, cancellationToken))
                {
                    var collidedRecord = await _idempotencyReadRepository.GetAsync(
                        idempotencyRequest.Key,
                        lockForUpdate: true,
                        cancellationToken)
                        ?? throw InProgress();

                    return ReadExistingResponse(collidedRecord, idempotencyRequest);
                }

                var response = await CaptureResponseAsync(context, next, cancellationToken);

                await _idempotencyWriteRepository.StoreResponseAsync(
                    idempotencyRequest.Key,
                    response.StatusCode,
                    ToStoredResponseBody(response),
                    cancellationToken);

                return response;
            },
            context.RequestAborted);
    }

    private async Task<IdempotencyResponse?> ReadExistingResponseAsync(IdempotencyRequest request, CancellationToken cancellationToken)
    {
        var record = await _idempotencyReadRepository.GetAsync(
            request.Key,
            lockForUpdate: false,
            cancellationToken);

        return record is null ? null : ReadExistingResponse(record, request);
    }

    private static IdempotencyResponse ReadExistingResponse(IdempotencyRecord record, IdempotencyRequest request)
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

        var responseCode = record.ResponseCode.Value;
        return ResponseMustNotIncludeBody(responseCode)
            ? new IdempotencyResponse(responseCode, null, string.Empty)
            : new IdempotencyResponse(responseCode, "application/json", record.ResponseBody);
    }

    private static async Task<IdempotencyResponse> CaptureResponseAsync(HttpContext context, RequestDelegate next, CancellationToken cancellationToken)
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

        if (!ResponseMustNotIncludeBody(idempotencyResponse.StatusCode)
            && !string.IsNullOrEmpty(idempotencyResponse.Body))
        {
            await response.WriteAsync(idempotencyResponse.Body, cancellationToken);
        }
    }

    private static string ToStoredResponseBody(IdempotencyResponse response) =>
        string.IsNullOrWhiteSpace(response.Body) || ResponseMustNotIncludeBody(response.StatusCode)
            ? "null"
            : response.Body;

    private static bool ResponseMustNotIncludeBody(int statusCode) =>
        statusCode is StatusCodes.Status204NoContent
            or StatusCodes.Status205ResetContent
            or StatusCodes.Status304NotModified;

    private static ServiceException Conflict(string message) =>
        new ApplicationServiceException(ServiceStatusErrorCodes.Conflict, message);

    private static ServiceException InProgress() =>
        new ApplicationServiceException(ServiceStatusErrorCodes.Conflict, "Idempotent request is already in progress.");

    private sealed record IdempotencyResponse(
        int StatusCode,
        string? ContentType,
        string Body);
}

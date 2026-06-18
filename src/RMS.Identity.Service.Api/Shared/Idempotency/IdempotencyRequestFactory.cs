using System.Text;
using System.Text.Json;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.Idempotency;
using RMS.Identity.Service.Domain.Interfaces.Security;

namespace RMS.Identity.Service.Api.Shared.Idempotency;

internal static class IdempotencyRequestFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<IdempotencyRequest> CreateAsync(
        HttpRequest request,
        ITextHasher textHasher,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = ParseIdempotencyKey(request);
        var requestBody = await ReadRequestBodyAsync(request, cancellationToken);
        var requestHash = CreateRequestHash(request, requestBody, textHasher);

        return new IdempotencyRequest(
            idempotencyKey.ToString(),
            request.Method,
            request.Path.Value ?? string.Empty,
            requestHash);
    }

    private static Guid ParseIdempotencyKey(HttpRequest request)
    {
        var idempotencyKey = request.Headers[IdempotencyHttpHeaders.HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ApplicationServiceException(ServiceStatusErrorCodes.BadRequest, "Idempotency-Key is required.");
        }

        if (!Guid.TryParse(idempotencyKey, out var parsedIdempotencyKey) || parsedIdempotencyKey == Guid.Empty)
        {
            throw new ApplicationServiceException(ServiceStatusErrorCodes.BadRequest, "Idempotency-Key must be a valid UUID.");
        }

        return parsedIdempotencyKey;
    }

    private static string CreateRequestHash(HttpRequest request, string body, ITextHasher textHasher) =>
        textHasher.Hash(JsonSerializer.Serialize(new
        {
            path = request.Path.Value ?? string.Empty,
            queryString = request.QueryString.Value ?? string.Empty,
            body
        }, JsonOptions));

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
}

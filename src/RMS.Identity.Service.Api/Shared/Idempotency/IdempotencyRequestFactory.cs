using System.Text;
using System.Text.Json;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.Idempotency;
using RMS.Identity.Service.Domain.Interfaces.Security;

namespace RMS.Identity.Service.Api.Shared.Idempotency;

internal sealed class IdempotencyRequestFactory : IIdempotencyRequestFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ITextHasher _textHasher;

    public IdempotencyRequestFactory(ITextHasher textHasher)
    {
        _textHasher = textHasher;
    }

    public async Task<IdempotencyRequest> CreateAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        var idempotencyKey = ParseIdempotencyKey(request);
        var requestBody = await ReadRequestBodyAsync(request, cancellationToken);
        var requestHash = CreateRequestHash(request, requestBody);

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
            throw new ServiceException(400, "VALIDATION_ERROR", "Idempotency-Key is required.");
        }

        if (!Guid.TryParse(idempotencyKey, out var parsedIdempotencyKey) || parsedIdempotencyKey == Guid.Empty)
        {
            throw new ServiceException(400, "VALIDATION_ERROR", "Idempotency-Key must be a valid UUID.");
        }

        return parsedIdempotencyKey;
    }

    private string CreateRequestHash(HttpRequest request, string body) =>
        _textHasher.Hash(JsonSerializer.Serialize(new
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

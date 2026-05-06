using System.Text.Json;
using RMS.Identity.Service.Domain.Contracts.Idempotency;
using RMS.Identity.Service.Domain.Interfaces.Security;

namespace RMS.Identity.Service.Application.Shared.Idempotency;

public sealed class IdempotencyRequestFactory : IIdempotencyRequestFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ITextHasher _textHasher;

    public IdempotencyRequestFactory(ITextHasher textHasher)
    {
        _textHasher = textHasher;
    }

    public IdempotencyRequest Create<TRequest>(
        Guid idempotencyKey,
        string method,
        string route,
        TRequest request)
        where TRequest : notnull
    {
        var requestHash = _textHasher.Hash(JsonSerializer.Serialize(request, JsonOptions));
        return new IdempotencyRequest(idempotencyKey.ToString(), method, route, requestHash);
    }
}

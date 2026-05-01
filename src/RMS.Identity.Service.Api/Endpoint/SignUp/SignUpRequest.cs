using RMS.Identity.Service.Application.Shared.Errors;

namespace RMS.Identity.Service.Api.Endpoint.SignUp;

public sealed class SignUpRequest
{
    public const string IdempotencyKeyHeaderName = "Idempotency-Key";

    public SignUpRequest(Guid idempotencyKey, SignUpRequestBody body)
    {
        IdempotencyKey = idempotencyKey;
        Body = body;
    }

    public Guid IdempotencyKey { get; }

    public SignUpRequestBody Body { get; }

    public static SignUpRequest FromHttpRequest(HttpRequest httpRequest, SignUpRequestBody body)
    {
        var idempotencyKey = httpRequest.Headers[IdempotencyKeyHeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ServiceException(400, "VALIDATION_ERROR", "Idempotency-Key is required.");
        }

        if (!Guid.TryParse(idempotencyKey, out var parsedIdempotencyKey))
        {
            throw new ServiceException(400, "VALIDATION_ERROR", "Idempotency-Key must be a valid UUID.");
        }

        return new SignUpRequest(parsedIdempotencyKey, body);
    }
}

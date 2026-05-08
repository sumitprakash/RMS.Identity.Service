namespace RMS.Identity.Service.Api.Shared.Idempotency;

public interface IIdempotencyHttpMethodPolicy
{
    bool RequiresIdempotency(string method);
}

internal sealed class IdempotencyHttpMethodPolicy : IIdempotencyHttpMethodPolicy
{
    private static readonly HashSet<string> MethodsRequiringIdempotency = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Patch,
        HttpMethods.Delete
    };

    public bool RequiresIdempotency(string method) =>
        MethodsRequiringIdempotency.Contains(method);
}

namespace RMS.Identity.Service.Api.Shared.Idempotency;

public interface IIdempotencyHttpMethodPolicy
{
    bool RequiresIdempotency(string method);
}

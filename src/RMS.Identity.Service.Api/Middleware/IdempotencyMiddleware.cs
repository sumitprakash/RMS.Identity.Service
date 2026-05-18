using RMS.Identity.Service.Api.Shared.Idempotency;

namespace RMS.Identity.Service.Api.Middleware;

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
        IIdempotencyService idempotencyService)
    {
        if (!RequiresIdempotency(context.Request.Method))
        {
            await _next(context);
            return;
        }

        await idempotencyService.ExecuteAsync(context, _next, context.RequestAborted);
    }

    private static bool RequiresIdempotency(string method) =>
        MethodsRequiringIdempotency.Contains(method);
}

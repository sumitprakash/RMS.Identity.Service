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
        IIdempotencyTransactionPipeline transactionPipeline)
    {
        if (!RequiresIdempotency(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var idempotencyRequest = await requestFactory.CreateAsync(context.Request, context.RequestAborted);
        var middlewareResponse = await transactionPipeline.ExecuteAsync(
            context,
            idempotencyRequest,
            _next,
            context.RequestAborted);

        await middlewareResponse.WriteToAsync(context.Response, context.RequestAborted);
    }

    private static bool RequiresIdempotency(string method) =>
        MethodsRequiringIdempotency.Contains(method);
}

namespace RMS.Identity.Service.Api.Shared.Idempotency;

public sealed class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;

    public IdempotencyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IIdempotencyHttpMethodPolicy methodPolicy,
        IIdempotencyRequestFactory requestFactory,
        IIdempotencyTransactionPipeline transactionPipeline)
    {
        if (!methodPolicy.RequiresIdempotency(context.Request.Method))
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
}

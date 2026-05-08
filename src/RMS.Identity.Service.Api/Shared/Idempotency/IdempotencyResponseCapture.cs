using System.Text;

namespace RMS.Identity.Service.Api.Shared.Idempotency;

public interface IIdempotencyResponseCapture
{
    Task<IdempotencyMiddlewareResponse> CaptureAsync(
        HttpContext context,
        RequestDelegate next,
        CancellationToken cancellationToken);
}

internal sealed class IdempotencyResponseCapture : IIdempotencyResponseCapture
{
    public async Task<IdempotencyMiddlewareResponse> CaptureAsync(
        HttpContext context,
        RequestDelegate next,
        CancellationToken cancellationToken)
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

        return new IdempotencyMiddlewareResponse(
            context.Response.StatusCode,
            context.Response.ContentType,
            responseBody);
    }
}

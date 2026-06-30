using System.Diagnostics;
using Microsoft.Extensions.Options;
using RMS.Identity.Service.Api.Shared.Correlation;

namespace RMS.Identity.Service.Api.Middleware;

public sealed class CorrelationTraceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly CorrelationTraceOptions _options;
    private readonly ILogger<CorrelationTraceMiddleware> _logger;

    public CorrelationTraceMiddleware(
        RequestDelegate next,
        IOptions<CorrelationTraceOptions> options,
        ILogger<CorrelationTraceMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationTraceId = ResolveCorrelationTraceId();
        context.TraceIdentifier = correlationTraceId;
        context.Items[CorrelationTraceContext.ItemKey] = correlationTraceId;
        context.Response.Headers[_options.ResponseHeaderName] = correlationTraceId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[_options.ResponseHeaderName] = correlationTraceId;
            return Task.CompletedTask;
        });

        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationTraceId"] = correlationTraceId
        });

        await _next(context);
        stopwatch.Stop();

        _logger.LogInformation(
            "Processed {Method} {Path} with status {StatusCode} in {ElapsedMilliseconds} ms.",
            context.Request.Method,
            context.Request.Path.Value,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds);
    }

    private static string ResolveCorrelationTraceId()
    {
        var activityTraceId = Activity.Current?.TraceId.ToString();
        return !string.IsNullOrWhiteSpace(activityTraceId)
            ? activityTraceId
            : Guid.NewGuid().ToString("N");
    }
}

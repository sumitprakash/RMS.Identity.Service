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
        var correlationTraceId = ResolveCorrelationTraceId(context);
        var originalTraceIdentifier = context.TraceIdentifier;
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
            ["CorrelationTraceId"] = correlationTraceId,
            ["RequestTraceId"] = originalTraceIdentifier,
            ["ActivityTraceId"] = Activity.Current?.TraceId.ToString(),
            ["ActivitySpanId"] = Activity.Current?.SpanId.ToString()
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

    private string ResolveCorrelationTraceId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(_options.RequestHeaderName, out var headerValues))
        {
            var headerValue = headerValues.FirstOrDefault();
            if (IsValidCorrelationTraceId(headerValue))
            {
                return headerValue!;
            }
        }

        var activityTraceId = Activity.Current?.TraceId.ToString();
        return !string.IsNullOrWhiteSpace(activityTraceId)
            ? activityTraceId
            : Guid.NewGuid().ToString("N");
    }

    private bool IsValidCorrelationTraceId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length > _options.MaxLength)
        {
            return false;
        }

        return value.All(static character =>
            char.IsAsciiLetterOrDigit(character)
            || character is '-' or '_' or '.' or ':');
    }
}

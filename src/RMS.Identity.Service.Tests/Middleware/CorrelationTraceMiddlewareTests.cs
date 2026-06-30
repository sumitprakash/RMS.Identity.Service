using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RMS.Identity.Service.Api.Middleware;
using RMS.Identity.Service.Api.Shared.Correlation;

namespace RMS.Identity.Service.Tests.Middleware;

public sealed class CorrelationTraceMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithIncomingCorrelationTrace_IgnoresClientValueAndReturnsGeneratedTrace()
    {
        const string clientCorrelationTraceId = "client-trace-1";
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-Trace-ID"] = clientCorrelationTraceId;
        context.Response.Body = new MemoryStream();
        var middleware = new CorrelationTraceMiddleware(
            async nextContext =>
            {
                Assert.False(string.IsNullOrWhiteSpace(nextContext.TraceIdentifier));
                Assert.NotEqual(clientCorrelationTraceId, nextContext.TraceIdentifier);
                Assert.Equal(nextContext.TraceIdentifier, CorrelationTraceContext.GetCorrelationTraceId(nextContext));
                await nextContext.Response.WriteAsync("ok");
            },
            Options.Create(new CorrelationTraceOptions()),
            NullLogger<CorrelationTraceMiddleware>.Instance);

        await middleware.InvokeAsync(context);
        await context.Response.StartAsync();

        var responseCorrelationTraceId = context.Response.Headers["X-Correlation-Trace-ID"].ToString();
        Assert.NotEqual(clientCorrelationTraceId, responseCorrelationTraceId);
        Assert.False(string.IsNullOrWhiteSpace(responseCorrelationTraceId));
    }

    [Fact]
    public async Task InvokeAsync_WithoutIncomingCorrelationTrace_GeneratesCorrelationTrace()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new CorrelationTraceMiddleware(
            async nextContext =>
            {
                Assert.False(string.IsNullOrWhiteSpace(nextContext.TraceIdentifier));
                Assert.Equal(nextContext.TraceIdentifier, CorrelationTraceContext.GetCorrelationTraceId(nextContext));
                await nextContext.Response.WriteAsync("ok");
            },
            Options.Create(new CorrelationTraceOptions()),
            NullLogger<CorrelationTraceMiddleware>.Instance);

        await middleware.InvokeAsync(context);
        await context.Response.StartAsync();

        Assert.False(string.IsNullOrWhiteSpace(context.Response.Headers["X-Correlation-Trace-ID"]));
    }
}

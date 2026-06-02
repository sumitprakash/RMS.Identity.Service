using Microsoft.AspNetCore.Http;
using RMS.Identity.Service.Api.Middleware;
using RMS.Identity.Service.Api.Shared.Idempotency;

namespace RMS.Identity.Service.Tests.Middleware;

public sealed class IdempotencyMiddlewareTests
{
    [Theory]
    [InlineData("/api/v1/auth/login")]
    [InlineData("/api/v1/auth/refresh")]
    public async Task InvokeAsync_ForAuthSessionEndpoints_BypassesIdempotency(string path)
    {
        var nextCalled = false;
        var middleware = new IdempotencyMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = path;
        var idempotencyService = new FakeIdempotencyService();

        await middleware.InvokeAsync(context, idempotencyService);

        Assert.True(nextCalled);
        Assert.False(idempotencyService.ExecuteCalled);
    }

    [Fact]
    public async Task InvokeAsync_ForOtherPost_UsesIdempotency()
    {
        var nextCalled = false;
        var middleware = new IdempotencyMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/v1/signup";
        var idempotencyService = new FakeIdempotencyService();

        await middleware.InvokeAsync(context, idempotencyService);

        Assert.True(idempotencyService.ExecuteCalled);
        Assert.True(nextCalled);
    }

    private sealed class FakeIdempotencyService : IIdempotencyService
    {
        public bool ExecuteCalled { get; private set; }

        public Task ExecuteAsync(
            HttpContext context,
            RequestDelegate next,
            CancellationToken cancellationToken)
        {
            ExecuteCalled = true;
            return next(context);
        }
    }
}

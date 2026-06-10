using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using RMS.Identity.Service.Api.Shared.Idempotency;
using RMS.Identity.Service.Domain.Contracts.Idempotency;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Idempotency;
using RMS.Identity.Service.Domain.Interfaces.Security;

namespace RMS.Identity.Service.Tests.Shared.Idempotency;

public sealed class IdempotencyServiceTests
{
    [Fact]
    public async Task ExecuteAsync_WhenNoContentResponseIsReplayed_DoesNotWriteNullBody()
    {
        var repository = new InMemoryIdempotencyRepository();
        var service = new IdempotencyService(
            new Sha256TextHasher(),
            new InlineTransactionExecutor(),
            repository,
            repository);
        var idempotencyKey = Guid.NewGuid().ToString();
        var nextCalls = 0;

        await ExecuteDeleteAsync(service, idempotencyKey, _ =>
        {
            nextCalls++;
            return Task.CompletedTask;
        });

        var replayBody = await ExecuteDeleteAsync(service, idempotencyKey, _ =>
        {
            nextCalls++;
            return Task.CompletedTask;
        });

        Assert.Equal(1, nextCalls);
        Assert.Equal("null", repository.StoredResponseBody);
        Assert.Empty(replayBody);
    }

    private static async Task<string> ExecuteDeleteAsync(
        IdempotencyService service,
        string idempotencyKey,
        RequestDelegate next)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Delete;
        context.Request.Path = "/api/v1/companies/00000000-0000-0000-0000-000000000001/users/00000000-0000-0000-0000-000000000002";
        context.Request.Headers[IdempotencyHttpHeaders.HeaderName] = idempotencyKey;
        context.Response.Body = new MemoryStream();

        await service.ExecuteAsync(
            context,
            async httpContext =>
            {
                httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
                await next(httpContext);
            },
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        return await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();
    }

    private sealed class InlineTransactionExecutor : IDatabaseTransactionExecutor
    {
        public Task<TResult> ExecuteAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken) =>
            operation(cancellationToken);
    }

    private sealed class InMemoryIdempotencyRepository :
        IIdempotencyReadRepository,
        IIdempotencyWriteRepository
    {
        private IdempotencyRecord? _record;

        public string? StoredResponseBody { get; private set; }

        public Task<IdempotencyRecord?> GetAsync(
            string key,
            bool lockForUpdate,
            CancellationToken cancellationToken) =>
            Task.FromResult(_record);

        public Task<bool> TryCreateAsync(
            IdempotencyRequest request,
            CancellationToken cancellationToken)
        {
            if (_record is not null)
            {
                return Task.FromResult(false);
            }

            _record = new IdempotencyRecord(
                request.Method,
                request.Route,
                request.RequestHash,
                ResponseCode: null,
                ResponseBody: null);

            return Task.FromResult(true);
        }

        public Task StoreResponseAsync(
            string key,
            int responseCode,
            string responseBody,
            CancellationToken cancellationToken)
        {
            StoredResponseBody = responseBody;
            _record = _record! with
            {
                ResponseCode = responseCode,
                ResponseBody = responseBody
            };

            return Task.CompletedTask;
        }
    }

    private sealed class Sha256TextHasher : ITextHasher
    {
        public string Hash(string value) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }
}

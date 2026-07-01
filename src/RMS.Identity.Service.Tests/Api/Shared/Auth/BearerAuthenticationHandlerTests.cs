using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RMS.Identity.Service.Api.Shared.Auth;

namespace RMS.Identity.Service.Tests.Api.Shared.Auth;

public sealed class BearerAuthenticationHandlerTests
{
    [Fact]
    public async Task AuthenticateAsync_WithResolvedUsername_UsesUsernameForNameClaim()
    {
        var userUuid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var handler = new BearerAuthenticationHandler(
            new StaticOptionsMonitor<AuthenticationSchemeOptions>(new AuthenticationSchemeOptions()),
            NullLoggerFactory.Instance,
            UrlEncoder.Default,
            new StubAccessTokenUserResolver(new AccessTokenUser(userUuid, "alice@example.com")));
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer test-token";

        await handler.InitializeAsync(
            new AuthenticationScheme("Bearer", "Bearer", typeof(BearerAuthenticationHandler)),
            context);

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(userUuid.ToString(), result.Principal!.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal(userUuid.ToString(), result.Principal.FindFirstValue("sub"));
        Assert.Equal("alice@example.com", result.Principal.FindFirstValue(ClaimTypes.Name));
    }

    private sealed class StubAccessTokenUserResolver : IAccessTokenUserResolver
    {
        private readonly AccessTokenUser _user;

        public StubAccessTokenUserResolver(AccessTokenUser user)
        {
            _user = user;
        }

        public Guid ResolveRequiredUserUuid(HttpContext context) => _user.UserUuid;

        public AccessTokenUser ResolveRequiredUser(HttpContext context) => _user;
    }

    private sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T>
    {
        private readonly T _value;

        public StaticOptionsMonitor(T value)
        {
            _value = value;
        }

        public T CurrentValue => _value;

        public T Get(string? name) => _value;

        public IDisposable OnChange(Action<T, string?> listener) => EmptyDisposable.Instance;
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public static readonly EmptyDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}

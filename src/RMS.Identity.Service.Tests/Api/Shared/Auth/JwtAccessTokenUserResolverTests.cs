using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using RMS.Identity.Service.Api.Shared.Auth;
using RMS.Identity.Service.Domain.Entities.Auth;
using RMS.Identity.Service.Domain.Interfaces.Security;
using RMS.Identity.Service.Infrastructure.Security;

namespace RMS.Identity.Service.Tests.Api.Shared.Auth;

public sealed class JwtAccessTokenUserResolverTests
{
    [Fact]
    public void ResolveRequiredUser_WithUsernameClaim_ReturnsUuidAndUsername()
    {
        var options = CreateOptions();
        var userUuid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var tokenGenerator = new JwtAuthTokenGenerator(
            Options.Create(options),
            new StubRefreshTokenGenerator());
        var tokens = tokenGenerator.Generate(CreateUser(userUuid, "alice@example.com"));
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = $"Bearer {tokens.AccessToken}";
        var resolver = new JwtAccessTokenUserResolver(Options.Create(options));

        var user = resolver.ResolveRequiredUser(context);

        Assert.Equal(userUuid, user.UserUuid);
        Assert.Equal("alice@example.com", user.Username);
    }

    private static JwtOptions CreateOptions() =>
        new()
        {
            Issuer = "RMS.Identity.Service.Tests",
            Audience = "RMS.Identity.Service.Tests",
            SigningKey = "test-signing-key-with-at-least-thirty-two-bytes",
            AccessTokenLifetimeSeconds = 3600,
            RefreshTokenLifetimeDays = 30
        };

    private static AuthenticatedUser CreateUser(Guid userUuid, string username) =>
        new(
            10,
            userUuid,
            null,
            username,
            "password-hash",
            "Alice Example",
            EmailVerified: true,
            IsActive: true,
            IsDeleted: false,
            LockedUntil: null,
            DateTime.UtcNow,
            new[] { "COMPANY_ADMIN" });

    private sealed class StubRefreshTokenGenerator : IRefreshTokenGenerator
    {
        public string Generate() => "refresh-token";
    }
}

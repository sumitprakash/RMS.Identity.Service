using System.Net;
using RMS.Identity.Service.Application.Commands.Refresh;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.Refresh;
using RMS.Identity.Service.Domain.Entities.Auth;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Auth;
using RMS.Identity.Service.Domain.Interfaces.Security;

namespace RMS.Identity.Service.Tests.Application.Commands.Refresh;

public sealed class RefreshCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidRefreshToken_RotatesTokenAndReturnsNewTokens()
    {
        var user = CreateUser();
        var session = new RefreshTokenSession(
            22,
            "hash:refresh-token",
            DateTime.UtcNow.AddDays(1),
            RevokedAt: null,
            user);
        var repository = new FakeAuthenticationRepository(session);
        var handler = new RefreshCommandHandler(
            repository,
            new FakeAuthTokenGenerator(),
            new FakeTextHasher());

        var response = await handler.HandleAsync(
            new RefreshCommandRequest("refresh-token"),
            CancellationToken.None);

        Assert.Equal("new-access-token", response.AccessToken);
        Assert.Equal("new-refresh-token", response.RefreshToken);
        Assert.Equal(3600, response.ExpiresIn);
        Assert.Equal("hash:refresh-token", repository.RequestedRefreshTokenHash);
        Assert.Equal(22, repository.RotatedRefreshTokenId);
        Assert.Equal(user.UserId, repository.RotatedUserId);
        Assert.Equal("hash:new-refresh-token", repository.NewRefreshTokenHash);
        Assert.NotNull(repository.NewRefreshTokenExpiresAt);
    }

    [Fact]
    public async Task HandleAsync_WithMissingRefreshToken_ThrowsUnauthorized()
    {
        var handler = new RefreshCommandHandler(
            new FakeAuthenticationRepository(session: null),
            new FakeAuthTokenGenerator(),
            new FakeTextHasher());

        var exception = await Assert.ThrowsAsync<ServiceException>(() =>
            handler.HandleAsync(new RefreshCommandRequest("refresh-token"), CancellationToken.None));

        Assert.Equal((int)HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Equal("INVALID_REFRESH_TOKEN", exception.Code);
    }

    [Fact]
    public async Task HandleAsync_WithRevokedRefreshToken_ThrowsUnauthorized()
    {
        var handler = new RefreshCommandHandler(
            new FakeAuthenticationRepository(new RefreshTokenSession(
                22,
                "hash:refresh-token",
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow,
                CreateUser())),
            new FakeAuthTokenGenerator(),
            new FakeTextHasher());

        var exception = await Assert.ThrowsAsync<ServiceException>(() =>
            handler.HandleAsync(new RefreshCommandRequest("refresh-token"), CancellationToken.None));

        Assert.Equal((int)HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Equal("INVALID_REFRESH_TOKEN", exception.Code);
    }

    [Fact]
    public async Task HandleAsync_WhenRotationLosesRace_ThrowsUnauthorized()
    {
        var repository = new FakeAuthenticationRepository(new RefreshTokenSession(
            22,
            "hash:refresh-token",
            DateTime.UtcNow.AddDays(1),
            RevokedAt: null,
            CreateUser()))
        {
            RotationResult = false
        };
        var handler = new RefreshCommandHandler(
            repository,
            new FakeAuthTokenGenerator(),
            new FakeTextHasher());

        var exception = await Assert.ThrowsAsync<ServiceException>(() =>
            handler.HandleAsync(new RefreshCommandRequest("refresh-token"), CancellationToken.None));

        Assert.Equal((int)HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Equal("INVALID_REFRESH_TOKEN", exception.Code);
    }

    private static AuthenticatedUser CreateUser() =>
        new(
            10,
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            null,
            "alice@example.com",
            "password-hash",
            "Alice Example",
            EmailVerified: true,
            IsActive: true,
            IsDeleted: false,
            LockedUntil: null,
            DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-1), DateTimeKind.Utc),
            new[] { "COMPANY_ADMIN" });

    private sealed class FakeAuthenticationRepository : IAuthenticationRepository
    {
        private readonly RefreshTokenSession? _session;

        public FakeAuthenticationRepository(RefreshTokenSession? session)
        {
            _session = session;
        }

        public bool RotationResult { get; init; } = true;

        public string? RequestedRefreshTokenHash { get; private set; }

        public long? RotatedRefreshTokenId { get; private set; }

        public long? RotatedUserId { get; private set; }

        public string? NewRefreshTokenHash { get; private set; }

        public DateTime? NewRefreshTokenExpiresAt { get; private set; }

        public Task<AuthenticatedUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken) =>
            Task.FromResult<AuthenticatedUser?>(null);

        public Task<RefreshTokenSession?> GetRefreshTokenSessionAsync(
            string refreshTokenHash,
            CancellationToken cancellationToken)
        {
            RequestedRefreshTokenHash = refreshTokenHash;
            return Task.FromResult(_session);
        }

        public Task RecordFailedLoginAsync(long userId, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task RecordSuccessfulLoginAsync(
            long userId,
            string refreshTokenHash,
            DateTime refreshTokenExpiresAt,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<bool> RotateRefreshTokenAsync(
            long refreshTokenId,
            long userId,
            string newRefreshTokenHash,
            DateTime newRefreshTokenExpiresAt,
            CancellationToken cancellationToken)
        {
            RotatedRefreshTokenId = refreshTokenId;
            RotatedUserId = userId;
            NewRefreshTokenHash = newRefreshTokenHash;
            NewRefreshTokenExpiresAt = newRefreshTokenExpiresAt;
            return Task.FromResult(RotationResult);
        }
    }

    private sealed class FakeAuthTokenGenerator : IAuthTokenGenerator
    {
        public AuthTokens Generate(AuthenticatedUser user) =>
            new(
                "new-access-token",
                "new-refresh-token",
                3600,
                DateTime.UtcNow.AddDays(30));
    }

    private sealed class FakeTextHasher : ITextHasher
    {
        public string Hash(string value) => $"hash:{value}";
    }
}

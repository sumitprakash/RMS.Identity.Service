using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using RMS.Identity.Service.Application.Commands.Login;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.Login;
using RMS.Identity.Service.Domain.Entities.Auth;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Auth;
using RMS.Identity.Service.Domain.Interfaces.Security;

namespace RMS.Identity.Service.Tests.Application.Commands.Login;

public sealed class LoginCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidCredentials_ReturnsTokensAndRecordsSuccessfulLogin()
    {
        var user = CreateUser();
        var repository = new FakeAuthenticationRepository(user);
        var handler = new LoginCommandHandler(
            repository,
            new FakePasswordHasher(validPassword: "StrongPass@123"),
            new FakeAuthTokenGenerator(),
            new FakeTextHasher(),
            NullLogger<LoginCommandHandler>.Instance);

        var response = await handler.HandleAsync(
            new LoginCommandRequest(" Alice@Example.com ", "StrongPass@123"),
            CancellationToken.None);

        Assert.Equal("access-token", response.AccessToken);
        Assert.Equal("refresh-token", response.RefreshToken);
        Assert.Equal(3600, response.ExpiresIn);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Equal(user.UserUuid, response.User.UserUuid);
        Assert.Equal("alice@example.com", repository.LastUsername);
        Assert.Equal("hash:refresh-token", repository.StoredRefreshTokenHash);
        Assert.NotNull(repository.StoredRefreshTokenExpiresAt);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidPassword_ThrowsUnauthorizedAndRecordsFailedLogin()
    {
        var user = CreateUser();
        var repository = new FakeAuthenticationRepository(user);
        var handler = new LoginCommandHandler(
            repository,
            new FakePasswordHasher(validPassword: "StrongPass@123"),
            new FakeAuthTokenGenerator(),
            new FakeTextHasher(),
            NullLogger<LoginCommandHandler>.Instance);

        var exception = await Assert.ThrowsAnyAsync<ServiceException>(() =>
            handler.HandleAsync(
                new LoginCommandRequest("alice@example.com", "wrong-password"),
                CancellationToken.None));

        Assert.Equal((int)HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Equal("401-2-1", exception.Code);
        Assert.Equal(user.UserId, repository.FailedLoginUserId);
    }

    [Fact]
    public async Task HandleAsync_WithUnverifiedEmail_ThrowsForbidden()
    {
        var repository = new FakeAuthenticationRepository(CreateUser(emailVerified: false));
        var handler = new LoginCommandHandler(
            repository,
            new FakePasswordHasher(validPassword: "StrongPass@123"),
            new FakeAuthTokenGenerator(),
            new FakeTextHasher(),
            NullLogger<LoginCommandHandler>.Instance);

        var exception = await Assert.ThrowsAnyAsync<ServiceException>(() =>
            handler.HandleAsync(
                new LoginCommandRequest("alice@example.com", "StrongPass@123"),
                CancellationToken.None));

        Assert.Equal((int)HttpStatusCode.Forbidden, exception.StatusCode);
        Assert.Equal("403-2-5", exception.Code);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownUsername_PerformsPasswordHashAndThrowsUnauthorized()
    {
        var repository = new FakeAuthenticationRepository(null);
        var passwordHasher = new FakePasswordHasher(validPassword: "StrongPass@123");
        var handler = new LoginCommandHandler(
            repository,
            passwordHasher,
            new FakeAuthTokenGenerator(),
            new FakeTextHasher(),
            NullLogger<LoginCommandHandler>.Instance);

        var exception = await Assert.ThrowsAnyAsync<ServiceException>(() =>
            handler.HandleAsync(
                new LoginCommandRequest("missing@example.com", "StrongPass@123"),
                CancellationToken.None));

        Assert.Equal((int)HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Equal("StrongPass@123", passwordHasher.LastHashedValue);
    }

    [Fact]
    public async Task HandleAsync_WithPasswordSetupRequired_ThrowsForbidden()
    {
        var repository = new FakeAuthenticationRepository(
            CreateUser() with { PasswordSetupRequired = true });
        var handler = new LoginCommandHandler(
            repository,
            new FakePasswordHasher(validPassword: "StrongPass@123"),
            new FakeAuthTokenGenerator(),
            new FakeTextHasher(),
            NullLogger<LoginCommandHandler>.Instance);

        var exception = await Assert.ThrowsAnyAsync<ServiceException>(() =>
            handler.HandleAsync(
                new LoginCommandRequest("alice@example.com", "StrongPass@123"),
                CancellationToken.None));

        Assert.Equal((int)HttpStatusCode.Forbidden, exception.StatusCode);
        Assert.Equal("403-2-10", exception.Code);
    }

    private static AuthenticatedUser CreateUser(bool emailVerified = true) =>
        new(
            10,
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            null,
            "alice@example.com",
            "password-hash",
            "Alice Example",
            emailVerified,
            IsActive: true,
            IsDeleted: false,
            LockedUntil: null,
            DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-1), DateTimeKind.Utc),
            new[] { "COMPANY_ADMIN" });

    private sealed class FakeAuthenticationRepository : IAuthenticationRepository
    {
        private readonly AuthenticatedUser? _user;

        public FakeAuthenticationRepository(AuthenticatedUser? user)
        {
            _user = user;
        }

        public string? LastUsername { get; private set; }

        public long? FailedLoginUserId { get; private set; }

        public string? StoredRefreshTokenHash { get; private set; }

        public DateTime? StoredRefreshTokenExpiresAt { get; private set; }

        public Task<AuthenticatedUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
        {
            LastUsername = username;
            return Task.FromResult(_user);
        }

        public Task<RefreshTokenSession?> GetRefreshTokenSessionAsync(
            string refreshTokenHash,
            CancellationToken cancellationToken) =>
            Task.FromResult<RefreshTokenSession?>(null);

        public Task RecordFailedLoginAsync(long userId, CancellationToken cancellationToken)
        {
            FailedLoginUserId = userId;
            return Task.CompletedTask;
        }

        public Task RecordSuccessfulLoginAsync(
            long userId,
            string refreshTokenHash,
            DateTime refreshTokenExpiresAt,
            CancellationToken cancellationToken)
        {
            StoredRefreshTokenHash = refreshTokenHash;
            StoredRefreshTokenExpiresAt = refreshTokenExpiresAt;
            return Task.CompletedTask;
        }

        public Task<bool> RotateRefreshTokenAsync(
            long refreshTokenId,
            long userId,
            string newRefreshTokenHash,
            DateTime newRefreshTokenExpiresAt,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        private readonly string _validPassword;

        public FakePasswordHasher(string validPassword)
        {
            _validPassword = validPassword;
        }

        public string? LastHashedValue { get; private set; }

        public string Hash(string value)
        {
            LastHashedValue = value;
            return "hash";
        }

        public bool Verify(string value, string hash) => value == _validPassword;
    }

    private sealed class FakeAuthTokenGenerator : IAuthTokenGenerator
    {
        public AuthTokens Generate(AuthenticatedUser user) =>
            new(
                "access-token",
                "refresh-token",
                3600,
                DateTime.UtcNow.AddDays(30));
    }

    private sealed class FakeTextHasher : ITextHasher
    {
        public string Hash(string value) => $"hash:{value}";
    }
}

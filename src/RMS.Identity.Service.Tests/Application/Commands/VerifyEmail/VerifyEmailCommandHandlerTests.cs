using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using RMS.Identity.Service.Application.Commands.VerifyEmail;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Entities.UserAccounts;
using RMS.Identity.Service.Domain.Entities.VerifyEmail;
using RMS.Identity.Service.Domain.Contracts.UserAccounts;
using RMS.Identity.Service.Domain.Contracts.VerifyEmail;
using RMS.Identity.Service.Domain.Interfaces.Repositories.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Repositories.VerifyEmail;
using RMS.Identity.Service.Domain.Interfaces.Security;

namespace RMS.Identity.Service.Tests.Application.Commands.VerifyEmail;

public sealed class VerifyEmailCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidToken_MarksUserVerifiedAndConsumesToken()
    {
        var userRepository = new FakeUserAccountRepository();
        var emailVerificationRepository = new FakeEmailVerificationRepository(CreateToken());
        var handler = new VerifyEmailCommandHandler(
            emailVerificationRepository,
            emailVerificationRepository,
            new FakePasswordHasher(),
            new FakeTextHasher(),
            userRepository,
            userRepository,
            NullLogger<VerifyEmailCommandHandler>.Instance);

        var response = await handler.HandleAsync(
            new VerifyEmailCommandRequest("valid-token"),
            CancellationToken.None);

        Assert.True(response.Success);
        Assert.Equal("Email verified successfully", response.Message);
        Assert.Equal(10, userRepository.VerifiedUserId);
        Assert.Equal(100, emailVerificationRepository.ConsumedEmailVerificationId);
    }

    [Fact]
    public async Task HandleAsync_WhenTokenConsumeDoesNotUpdate_ThrowsBadRequest()
    {
        var userRepository = new FakeUserAccountRepository();
        var emailVerificationRepository = new FakeEmailVerificationRepository(
            CreateToken(),
            consumeSucceeds: false);
        var handler = new VerifyEmailCommandHandler(
            emailVerificationRepository,
            emailVerificationRepository,
            new FakePasswordHasher(),
            new FakeTextHasher(),
            userRepository,
            userRepository,
            NullLogger<VerifyEmailCommandHandler>.Instance);

        var exception = await Assert.ThrowsAnyAsync<ServiceException>(() =>
            handler.HandleAsync(new VerifyEmailCommandRequest("valid-token"), CancellationToken.None));

        Assert.Equal((int)HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal("400-6-2", exception.Code);
        Assert.Equal(100, emailVerificationRepository.ConsumedEmailVerificationId);
        Assert.Null(userRepository.VerifiedUserId);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownToken_ThrowsNotFound()
    {
        var repository = new FakeEmailVerificationRepository(null);
        var handler = new VerifyEmailCommandHandler(
            repository,
            repository,
            new FakePasswordHasher(),
            new FakeTextHasher(),
            new FakeUserAccountRepository(),
            new FakeUserAccountRepository(),
            NullLogger<VerifyEmailCommandHandler>.Instance);

        var exception = await Assert.ThrowsAnyAsync<ServiceException>(() =>
            handler.HandleAsync(new VerifyEmailCommandRequest("missing-token"), CancellationToken.None));

        Assert.Equal((int)HttpStatusCode.NotFound, exception.StatusCode);
        Assert.Equal("404-6-1", exception.Code);
    }

    [Fact]
    public async Task HandleAsync_WithConsumedToken_ThrowsBadRequest()
    {
        var repository = new FakeEmailVerificationRepository(CreateToken(consumed: true));
        var handler = new VerifyEmailCommandHandler(
            repository,
            repository,
            new FakePasswordHasher(),
            new FakeTextHasher(),
            new FakeUserAccountRepository(),
            new FakeUserAccountRepository(),
            NullLogger<VerifyEmailCommandHandler>.Instance);

        var exception = await Assert.ThrowsAnyAsync<ServiceException>(() =>
            handler.HandleAsync(new VerifyEmailCommandRequest("valid-token"), CancellationToken.None));

        Assert.Equal((int)HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal("400-6-2", exception.Code);
    }

    [Fact]
    public async Task HandleAsync_WithExpiredToken_ThrowsBadRequest()
    {
        var repository = new FakeEmailVerificationRepository(CreateToken(expiresAt: DateTime.UtcNow.AddMinutes(-1)));
        var handler = new VerifyEmailCommandHandler(
            repository,
            repository,
            new FakePasswordHasher(),
            new FakeTextHasher(),
            new FakeUserAccountRepository(),
            new FakeUserAccountRepository(),
            NullLogger<VerifyEmailCommandHandler>.Instance);

        var exception = await Assert.ThrowsAnyAsync<ServiceException>(() =>
            handler.HandleAsync(new VerifyEmailCommandRequest("valid-token"), CancellationToken.None));

        Assert.Equal((int)HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal("400-6-3", exception.Code);
    }

    [Fact]
    public async Task HandleAsync_ForInvitedUser_WithPassword_CompletesPasswordSetup()
    {
        var userRepository = new FakeUserAccountRepository(passwordSetupRequired: true);
        var emailVerificationRepository = new FakeEmailVerificationRepository(CreateToken());
        var handler = new VerifyEmailCommandHandler(
            emailVerificationRepository,
            emailVerificationRepository,
            new FakePasswordHasher(),
            new FakeTextHasher(),
            userRepository,
            userRepository,
            NullLogger<VerifyEmailCommandHandler>.Instance);

        var response = await handler.HandleAsync(
            new VerifyEmailCommandRequest("valid-token", "StrongPass@123"),
            CancellationToken.None);

        Assert.True(response.Success);
        Assert.Equal(10, userRepository.PasswordSetupUserId);
        Assert.Equal("hash:StrongPass@123", userRepository.PasswordHash);
        Assert.Null(userRepository.VerifiedUserId);
    }

    [Fact]
    public async Task HandleAsync_ForInvitedUser_WithoutPassword_ThrowsBadRequest()
    {
        var userRepository = new FakeUserAccountRepository(passwordSetupRequired: true);
        var emailVerificationRepository = new FakeEmailVerificationRepository(CreateToken());
        var handler = new VerifyEmailCommandHandler(
            emailVerificationRepository,
            emailVerificationRepository,
            new FakePasswordHasher(),
            new FakeTextHasher(),
            userRepository,
            userRepository,
            NullLogger<VerifyEmailCommandHandler>.Instance);

        var exception = await Assert.ThrowsAnyAsync<ServiceException>(() =>
            handler.HandleAsync(new VerifyEmailCommandRequest("valid-token"), CancellationToken.None));

        Assert.Equal((int)HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Null(emailVerificationRepository.ConsumedEmailVerificationId);
    }

    private static EmailVerificationToken CreateToken(
        bool consumed = false,
        DateTime? expiresAt = null) =>
        new(
            100,
            10,
            "hash:valid-token",
            "email_verification",
            expiresAt ?? DateTime.UtcNow.AddHours(1),
            consumed);

    private sealed class FakeEmailVerificationRepository : IEmailVerificationReadRepository, IEmailVerificationWriteRepository
    {
        private readonly EmailVerificationToken? _token;
        private readonly bool _consumeSucceeds;

        public FakeEmailVerificationRepository(
            EmailVerificationToken? token,
            bool consumeSucceeds = true)
        {
            _token = token;
            _consumeSucceeds = consumeSucceeds;
        }

        public long? ConsumedEmailVerificationId { get; private set; }

        public Task<EmailVerificationToken?> GetByTokenHashAsync(
            string tokenHash,
            string purpose,
            CancellationToken cancellationToken) =>
            Task.FromResult(_token is not null && _token.TokenHash == tokenHash && _token.Purpose == purpose ? _token : null);

        public Task CreateAsync(CreateEmailVerificationCommand command, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> TryConsumeAsync(long emailVerificationId, CancellationToken cancellationToken)
        {
            ConsumedEmailVerificationId = emailVerificationId;
            return Task.FromResult(_consumeSucceeds);
        }
    }

    private sealed class FakeUserAccountRepository : IUserAccountReadRepository, IUserAccountWriteRepository
    {
        private readonly bool _passwordSetupRequired;

        public FakeUserAccountRepository(bool passwordSetupRequired = false)
        {
            _passwordSetupRequired = passwordSetupRequired;
        }

        public long? VerifiedUserId { get; private set; }

        public long? PasswordSetupUserId { get; private set; }

        public string? PasswordHash { get; private set; }

        public Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<long> CreateAsync(CreateUserAccountCommand command, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task MarkEmailVerifiedAsync(long userId, CancellationToken cancellationToken)
        {
            VerifiedUserId = userId;
            return Task.CompletedTask;
        }

        public Task CompletePasswordSetupAsync(
            long userId,
            string passwordHash,
            CancellationToken cancellationToken)
        {
            PasswordSetupUserId = userId;
            PasswordHash = passwordHash;
            return Task.CompletedTask;
        }

        public Task<UserAccount> GetByIdAsync(long userId, CancellationToken cancellationToken) =>
            Task.FromResult(new UserAccount(
                userId,
                Guid.NewGuid(),
                "alice@example.com",
                "Alice",
                EmailVerified: false,
                IsActive: true,
                IsDeleted: false,
                DateTime.UtcNow)
            {
                PasswordSetupRequired = _passwordSetupRequired
            });

        public Task<UserAccount> GetByUuidAsync(Guid userUuid, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeTextHasher : ITextHasher
    {
        public string Hash(string value) => $"hash:{value}";
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string value) => $"hash:{value}";

        public bool Verify(string value, string hash) => hash == Hash(value);
    }
}

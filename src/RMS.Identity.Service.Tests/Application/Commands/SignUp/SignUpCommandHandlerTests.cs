using Microsoft.Extensions.Options;
using RMS.Identity.Service.Application.Commands.SignUp;
using RMS.Identity.Service.Domain.Contracts.SignUp;
using RMS.Identity.Service.Domain.Contracts.UserAccounts;
using RMS.Identity.Service.Domain.Contracts.VerifyEmail;
using RMS.Identity.Service.Domain.Entities.Companies;
using RMS.Identity.Service.Domain.Entities.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Repositories.AuditLog;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Outbox;
using RMS.Identity.Service.Domain.Interfaces.Repositories.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Repositories.VerifyEmail;
using RMS.Identity.Service.Domain.Interfaces.Security;

namespace RMS.Identity.Service.Tests.Application.Commands.SignUp;

public sealed class SignUpCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidRequest_CreatesVerificationTokenAndOutboxEvent()
    {
        var userRepository = new FakeUserAccountRepository();
        var emailVerificationRepository = new FakeEmailVerificationWriteRepository();
        var outboxRepository = new FakeOutboxWriteRepository();
        var handler = new SignUpCommandHandler(
            userRepository,
            userRepository,
            new FakeAuditLogWriteRepository(),
            emailVerificationRepository,
            outboxRepository,
            new FakePasswordHasher(),
            new FakeTextHasher(),
            Options.Create(new EmailVerificationOptions()));

        var response = await handler.HandleAsync(
            new SignUpCommandRequest(
                " Alice@Example.com ",
                "StrongPass@123",
                "Alice",
                null,
                "Example",
                "+919876543210"),
            CancellationToken.None);

        Assert.Equal("alice@example.com", response.Username);
        Assert.Equal("pending", response.Status);
        Assert.NotNull(emailVerificationRepository.CreatedToken);
        Assert.Equal(10, emailVerificationRepository.CreatedToken.UserId);
        Assert.Equal("email_verification", emailVerificationRepository.CreatedToken.Purpose);
        Assert.StartsWith("hash:", emailVerificationRepository.CreatedToken.TokenHash);
        Assert.True(emailVerificationRepository.CreatedToken.ExpiresAt > DateTime.UtcNow.AddHours(23));
        Assert.NotNull(outboxRepository.Token);
        Assert.Equal(emailVerificationRepository.CreatedToken.TokenHash, $"hash:{outboxRepository.Token}");
        Assert.Equal(response.UserUuid, outboxRepository.Account?.UserUuid);
    }

    [Fact]
    public async Task HandleAsync_WhenAutoVerifyEnabled_MarksEmailVerifiedWithoutOutboxEvent()
    {
        var userRepository = new FakeUserAccountRepository();
        var emailVerificationRepository = new FakeEmailVerificationWriteRepository();
        var outboxRepository = new FakeOutboxWriteRepository();
        var handler = new SignUpCommandHandler(
            userRepository,
            userRepository,
            new FakeAuditLogWriteRepository(),
            emailVerificationRepository,
            outboxRepository,
            new FakePasswordHasher(),
            new FakeTextHasher(),
            Options.Create(new EmailVerificationOptions { AutoVerifyOnSignUp = true }));

        var response = await handler.HandleAsync(
            new SignUpCommandRequest(
                " Alice@Example.com ",
                "StrongPass@123",
                "Alice",
                null,
                "Example",
                "+919876543210"),
            CancellationToken.None);

        Assert.Equal("active", response.Status);
        Assert.Equal(10, userRepository.VerifiedUserId);
        Assert.Null(emailVerificationRepository.CreatedToken);
        Assert.Null(outboxRepository.Token);
    }

    private sealed class FakeUserAccountRepository : IUserAccountReadRepository, IUserAccountWriteRepository
    {
        private CreateUserAccountCommand? _createdUser;

        public long? VerifiedUserId { get; private set; }

        public Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<long> CreateAsync(CreateUserAccountCommand command, CancellationToken cancellationToken)
        {
            _createdUser = command;
            return Task.FromResult(10L);
        }

        public Task MarkEmailVerifiedAsync(long userId, CancellationToken cancellationToken)
        {
            VerifiedUserId = userId;
            return Task.CompletedTask;
        }

        public Task<UserAccount> GetByIdAsync(long userId, CancellationToken cancellationToken)
        {
            var createdUser = _createdUser ?? throw new InvalidOperationException("User was not created.");
            return Task.FromResult(new UserAccount(
                userId,
                createdUser.UserUuid,
                createdUser.Username,
                createdUser.DisplayName,
                EmailVerified: false,
                IsActive: true,
                IsDeleted: false,
                DateTime.UtcNow));
        }

        public Task<UserAccount> GetByUuidAsync(Guid userUuid, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeEmailVerificationWriteRepository : IEmailVerificationWriteRepository
    {
        public CreateEmailVerificationCommand? CreatedToken { get; private set; }

        public Task CreateAsync(CreateEmailVerificationCommand command, CancellationToken cancellationToken)
        {
            CreatedToken = command;
            return Task.CompletedTask;
        }

        public Task<bool> TryConsumeAsync(long emailVerificationId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeOutboxWriteRepository : IOutboxWriteRepository
    {
        public UserAccount? Account { get; private set; }

        public string? Token { get; private set; }

        public DateTime? ExpiresAt { get; private set; }

        public Task InsertEmailVerificationRequestedAsync(
            UserAccount account,
            string token,
            DateTime expiresAt,
            CancellationToken cancellationToken)
        {
            Account = account;
            Token = token;
            ExpiresAt = expiresAt;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAuditLogWriteRepository : IAuditLogWriteRepository
    {
        public Task InsertSignUpCreatedAsync(UserAccount account, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task InsertCompanyStatusChangedAsync(
            Company company,
            string previousStatus,
            long actorUserId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string value) => $"hashed:{value}";

        public bool Verify(string value, string hash) => hash == Hash(value);
    }

    private sealed class FakeTextHasher : ITextHasher
    {
        public string Hash(string value) => $"hash:{value}";
    }
}

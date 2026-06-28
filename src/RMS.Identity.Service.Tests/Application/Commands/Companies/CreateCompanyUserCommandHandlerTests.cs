using System.Net;
using RMS.Identity.Service.Application.Commands.Companies;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.Companies;
using RMS.Identity.Service.Domain.Contracts.CompanyUsers;
using RMS.Identity.Service.Domain.Contracts.UserAccounts;
using RMS.Identity.Service.Domain.Contracts.VerifyEmail;
using RMS.Identity.Service.Domain.Entities.Companies;
using RMS.Identity.Service.Domain.Entities.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Repositories.AuditLog;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.CompanyUsers;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Outbox;
using RMS.Identity.Service.Domain.Interfaces.Repositories.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Repositories.VerifyEmail;
using RMS.Identity.Service.Domain.Interfaces.Security;

namespace RMS.Identity.Service.Tests.Application.Commands.Companies;

public sealed class CreateCompanyUserCommandHandlerTests
{
    private static readonly Guid ActorUserUuid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CompanyUuid = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task HandleAsync_WithValidRequest_CreatesUserAndMembership()
    {
        var userRepository = new FakeUserAccountRepository();
        var companyUserRepository = new FakeCompanyUserWriteRepository();
        var emailVerificationRepository = new FakeEmailVerificationWriteRepository();
        var outboxRepository = new FakeOutboxWriteRepository();
        var auditRepository = new FakeAuditLogWriteRepository();
        var handler = new CreateCompanyUserCommandHandler(
            new FakeCompanyReadRepository(),
            userRepository,
            userRepository,
            companyUserRepository,
            emailVerificationRepository,
            outboxRepository,
            new FakePasswordHasher(),
            new FakeTextHasher(),
            auditRepository);

        var response = await handler.HandleAsync(
            new CreateCompanyUserCommandRequest(
                CompanyUuid,
                " Cashier@Example.com ",
                " Store Cashier ",
                CompanyRole.Member,
                ActorUserUuid),
            CancellationToken.None);

        Assert.Equal("cashier@example.com", response.Username);
        Assert.Equal("Store Cashier", response.DisplayName);
        Assert.Equal("MEMBER", response.CompanyRole);
        Assert.Equal("pending", response.Status);
        Assert.Equal("MEMBER", companyUserRepository.CreatedMembership?.CompanyRole);
        Assert.Equal("active", companyUserRepository.CreatedMembership?.MembershipStatus);
        Assert.Equal(100, companyUserRepository.CreatedMembership?.CompanyId);
        Assert.Equal(10, companyUserRepository.CreatedMembership?.UserId);
        Assert.NotNull(emailVerificationRepository.CreatedVerification);
        Assert.Equal(10, emailVerificationRepository.CreatedVerification.UserId);
        Assert.Equal("email_verification", emailVerificationRepository.CreatedVerification.Purpose);
        Assert.NotNull(outboxRepository.EmailVerificationAccount);
        Assert.Equal(response.UserUuid, outboxRepository.EmailVerificationAccount.UserUuid);
        Assert.Equal($"hashed-text:{outboxRepository.EmailVerificationToken}", emailVerificationRepository.CreatedVerification.TokenHash);
        Assert.True(userRepository.CreatedUser?.PasswordSetupRequired);
        Assert.Equal("company_user_created", auditRepository.Action);
        Assert.Equal(ActorUserUuid, auditRepository.ActorUserUuid);
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateUsername_ThrowsConflict()
    {
        var userRepository = new FakeUserAccountRepository(usernameExists: true);
        var handler = new CreateCompanyUserCommandHandler(
            new FakeCompanyReadRepository(),
            userRepository,
            userRepository,
            new FakeCompanyUserWriteRepository(),
            new FakeEmailVerificationWriteRepository(),
            new FakeOutboxWriteRepository(),
            new FakePasswordHasher(),
            new FakeTextHasher(),
            new FakeAuditLogWriteRepository());

        var exception = await Assert.ThrowsAnyAsync<ServiceException>(() =>
            handler.HandleAsync(
                new CreateCompanyUserCommandRequest(CompanyUuid, "cashier@example.com", null, CompanyRole.Member),
                CancellationToken.None));

        Assert.Equal((int)HttpStatusCode.Conflict, exception.StatusCode);
        Assert.Equal("409-3-2", exception.Code);
    }

    private sealed class FakeCompanyReadRepository : ICompanyReadRepository
    {
        public Task<bool> ExistsByGstinAsync(string gstin, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<Company> GetByIdAsync(long companyId, CancellationToken cancellationToken) =>
            Task.FromResult(CreateCompany(companyId, CompanyUuid));

        public Task<Company> GetByUuidAsync(Guid companyUuid, CancellationToken cancellationToken) =>
            Task.FromResult(CreateCompany(100, companyUuid));

        private static Company CreateCompany(long companyId, Guid companyUuid) =>
            new(
                companyId,
                companyUuid,
                "Example Retail Pvt Ltd",
                "Example Retail",
                "29ABCDE1234F1Z5",
                "accounts@example.com",
                "9876543211",
                "1 Main Road",
                null,
                "Bengaluru",
                "Karnataka",
                "560001",
                "IN",
                "pending_verification",
                IsDeleted: false,
                DateTime.UtcNow);
    }

    private sealed class FakeUserAccountRepository : IUserAccountReadRepository, IUserAccountWriteRepository
    {
        private readonly bool _usernameExists;
        private CreateUserAccountCommand? _createdUser;

        public CreateUserAccountCommand? CreatedUser => _createdUser;

        public FakeUserAccountRepository(bool usernameExists = false)
        {
            _usernameExists = usernameExists;
        }

        public Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken) =>
            Task.FromResult(_usernameExists);

        public Task<long> CreateAsync(CreateUserAccountCommand command, CancellationToken cancellationToken)
        {
            _createdUser = command;
            return Task.FromResult(10L);
        }

        public Task MarkEmailVerifiedAsync(long userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task CompletePasswordSetupAsync(
            long userId,
            string passwordHash,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

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

    private sealed class FakeCompanyUserWriteRepository : ICompanyUserWriteRepository
    {
        public CreateCompanyUserCommand? CreatedMembership { get; private set; }

        public Task CreateAsync(CreateCompanyUserCommand command, CancellationToken cancellationToken)
        {
            CreatedMembership = command;
            return Task.CompletedTask;
        }

        public Task<int> CountActiveOwnersForUpdateAsync(Guid companyUuid, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task UpdateMembershipAsync(UpdateCompanyUserCommand command, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeEmailVerificationWriteRepository : IEmailVerificationWriteRepository
    {
        public CreateEmailVerificationCommand? CreatedVerification { get; private set; }

        public Task CreateAsync(CreateEmailVerificationCommand command, CancellationToken cancellationToken)
        {
            CreatedVerification = command;
            return Task.CompletedTask;
        }

        public Task<bool> TryConsumeAsync(long emailVerificationId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeOutboxWriteRepository : IOutboxWriteRepository
    {
        public UserAccount? EmailVerificationAccount { get; private set; }
        public string? EmailVerificationToken { get; private set; }
        public DateTime? EmailVerificationExpiresAt { get; private set; }

        public Task InsertEmailVerificationRequestedAsync(
            UserAccount account,
            string token,
            DateTime expiresAt,
            CancellationToken cancellationToken)
        {
            EmailVerificationAccount = account;
            EmailVerificationToken = token;
            EmailVerificationExpiresAt = expiresAt;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string value) => $"hashed:{value}";

        public bool Verify(string value, string hash) => hash == Hash(value);
    }

    private sealed class FakeTextHasher : ITextHasher
    {
        public string Hash(string value) => $"hashed-text:{value}";
    }

    private sealed class FakeAuditLogWriteRepository : IAuditLogWriteRepository
    {
        public string? Action { get; private set; }

        public Guid? ActorUserUuid { get; private set; }

        public Task InsertSignUpCreatedAsync(UserAccount account, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task InsertCompanyStatusChangedAsync(
            Company company,
            string previousStatus,
            long actorUserId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task InsertCompanyUserChangedAsync(
            string action,
            Guid actorUserUuid,
            Guid companyUuid,
            Guid targetUserUuid,
            string? previousCompanyRole,
            string? previousMembershipStatus,
            string companyRole,
            string membershipStatus,
            CancellationToken cancellationToken)
        {
            Action = action;
            ActorUserUuid = actorUserUuid;
            return Task.CompletedTask;
        }
    }
}

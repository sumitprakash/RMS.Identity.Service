using System.Net;
using RMS.Identity.Service.Application.Commands.Companies;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.Companies;
using RMS.Identity.Service.Domain.Contracts.CompanyUsers;
using RMS.Identity.Service.Domain.Contracts.UserAccounts;
using RMS.Identity.Service.Domain.Entities.Companies;
using RMS.Identity.Service.Domain.Entities.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.CompanyUsers;
using RMS.Identity.Service.Domain.Interfaces.Repositories.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Security;

namespace RMS.Identity.Service.Tests.Application.Commands.Companies;

public sealed class CreateCompanyUserCommandHandlerTests
{
    private static readonly Guid CompanyUuid = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task HandleAsync_WithValidRequest_CreatesUserAndMembership()
    {
        var userRepository = new FakeUserAccountRepository();
        var companyUserRepository = new FakeCompanyUserWriteRepository();
        var handler = new CreateCompanyUserCommandHandler(
            new FakeCompanyReadRepository(),
            userRepository,
            userRepository,
            companyUserRepository,
            new FakePasswordHasher());

        var response = await handler.HandleAsync(
            new CreateCompanyUserCommandRequest(CompanyUuid, " Cashier@Example.com ", " Store Cashier ", "member"),
            CancellationToken.None);

        Assert.Equal("cashier@example.com", response.Username);
        Assert.Equal("Store Cashier", response.DisplayName);
        Assert.Equal("MEMBER", response.CompanyRole);
        Assert.Equal("pending", response.Status);
        Assert.Equal("MEMBER", companyUserRepository.CreatedMembership?.CompanyRole);
        Assert.Equal("active", companyUserRepository.CreatedMembership?.MembershipStatus);
        Assert.Equal(100, companyUserRepository.CreatedMembership?.CompanyId);
        Assert.Equal(10, companyUserRepository.CreatedMembership?.UserId);
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
            new FakePasswordHasher());

        var exception = await Assert.ThrowsAsync<ServiceException>(() =>
            handler.HandleAsync(
                new CreateCompanyUserCommandRequest(CompanyUuid, "cashier@example.com", null, "MEMBER"),
                CancellationToken.None));

        Assert.Equal((int)HttpStatusCode.Conflict, exception.StatusCode);
        Assert.Equal("USER_EXISTS", exception.Code);
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
                "+919876543211",
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

        public Task UpdateMembershipAsync(UpdateCompanyUserCommand command, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string value) => $"hashed:{value}";

        public bool Verify(string value, string hash) => hash == Hash(value);
    }
}

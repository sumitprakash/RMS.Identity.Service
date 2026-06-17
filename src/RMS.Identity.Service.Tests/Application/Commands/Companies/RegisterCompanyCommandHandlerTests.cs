using System.Net;
using RMS.Identity.Service.Application.Commands.Companies;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.Companies;
using RMS.Identity.Service.Domain.Contracts.CompanyUsers;
using RMS.Identity.Service.Domain.Entities.Companies;
using RMS.Identity.Service.Domain.Entities.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.CompanyUsers;
using RMS.Identity.Service.Domain.Interfaces.Repositories.UserAccounts;

namespace RMS.Identity.Service.Tests.Application.Commands.Companies;

public sealed class RegisterCompanyCommandHandlerTests
{
    private static readonly Guid OwnerUserUuid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task HandleAsync_WithValidRequest_CreatesCompanyAndOwnerMembership()
    {
        var userRepository = new FakeUserAccountReadRepository(CreateUser());
        var companyRepository = new FakeCompanyRepository();
        var companyUserRepository = new FakeCompanyUserWriteRepository();
        var handler = new RegisterCompanyCommandHandler(
            userRepository,
            companyRepository,
            companyRepository,
            companyUserRepository);

        var response = await handler.HandleAsync(CreateRequest(), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.CompanyUuid);
        Assert.Equal("Example Retail Pvt Ltd", response.LegalName);
        Assert.Equal("Example Retail", response.TradeName);
        Assert.Equal("29ABCDE1234F1Z5", response.Gstin);
        Assert.Equal("pending_verification", response.Status);
        Assert.Equal("29ABCDE1234F1Z5", companyRepository.CreatedCompany?.Gstin);
        Assert.Equal("accounts@example.com", companyRepository.CreatedCompany?.ContactEmailAddress);
        Assert.Equal("IN", companyRepository.CreatedCompany?.Country);
        Assert.Equal("OWNER", companyUserRepository.CreatedMembership?.CompanyRole);
        Assert.Equal("active", companyUserRepository.CreatedMembership?.MembershipStatus);
        Assert.Equal(100, companyUserRepository.CreatedMembership?.CompanyId);
        Assert.Equal(10, companyUserRepository.CreatedMembership?.UserId);
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateGstin_ThrowsConflictAndDoesNotCreateMembership()
    {
        var userRepository = new FakeUserAccountReadRepository(CreateUser());
        var companyRepository = new FakeCompanyRepository(gstinExists: true);
        var companyUserRepository = new FakeCompanyUserWriteRepository();
        var handler = new RegisterCompanyCommandHandler(
            userRepository,
            companyRepository,
            companyRepository,
            companyUserRepository);

        var exception = await Assert.ThrowsAnyAsync<ServiceException>(() =>
            handler.HandleAsync(CreateRequest(), CancellationToken.None));

        Assert.Equal((int)HttpStatusCode.Conflict, exception.StatusCode);
        Assert.Equal("409", exception.Code);
        Assert.Null(companyRepository.CreatedCompany);
        Assert.Null(companyUserRepository.CreatedMembership);
    }

    [Fact]
    public async Task HandleAsync_WithInactiveUser_ThrowsForbidden()
    {
        var userRepository = new FakeUserAccountReadRepository(CreateUser(isActive: false));
        var companyRepository = new FakeCompanyRepository();
        var handler = new RegisterCompanyCommandHandler(
            userRepository,
            companyRepository,
            companyRepository,
            new FakeCompanyUserWriteRepository());

        var exception = await Assert.ThrowsAnyAsync<ServiceException>(() =>
            handler.HandleAsync(CreateRequest(), CancellationToken.None));

        Assert.Equal((int)HttpStatusCode.Forbidden, exception.StatusCode);
        Assert.Equal("403", exception.Code);
    }

    private static RegisterCompanyCommandRequest CreateRequest() =>
        new(
            OwnerUserUuid,
            " Example Retail Pvt Ltd ",
            " Example Retail ",
            "29abcde1234f1z5",
            " Accounts@Example.com ",
            " +919876543211 ",
            " 1 Main Road ",
            null,
            " Bengaluru ",
            " Karnataka ",
            " 560001 ",
            " in ");

    private static UserAccount CreateUser(bool isActive = true) =>
        new(
            10,
            OwnerUserUuid,
            "owner@example.com",
            "Owner",
            EmailVerified: true,
            IsActive: isActive,
            IsDeleted: false,
            DateTime.UtcNow);

    private sealed class FakeUserAccountReadRepository : IUserAccountReadRepository
    {
        private readonly UserAccount _user;

        public FakeUserAccountReadRepository(UserAccount user)
        {
            _user = user;
        }

        public Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<UserAccount> GetByIdAsync(long userId, CancellationToken cancellationToken) =>
            Task.FromResult(_user);

        public Task<UserAccount> GetByUuidAsync(Guid userUuid, CancellationToken cancellationToken) =>
            Task.FromResult(_user);
    }

    private sealed class FakeCompanyRepository : ICompanyReadRepository, ICompanyWriteRepository
    {
        private readonly bool _gstinExists;

        public FakeCompanyRepository(bool gstinExists = false)
        {
            _gstinExists = gstinExists;
        }

        public CreateCompanyCommand? CreatedCompany { get; private set; }

        public Task<bool> ExistsByGstinAsync(string gstin, CancellationToken cancellationToken) =>
            Task.FromResult(_gstinExists);

        public Task<long> CreateAsync(CreateCompanyCommand command, CancellationToken cancellationToken)
        {
            CreatedCompany = command;
            return Task.FromResult(100L);
        }

        public Task UpdateAsync(UpdateCompanyCommand command, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task UpdateStatusAsync(UpdateCompanyStatusCommand command, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Company> GetByIdAsync(long companyId, CancellationToken cancellationToken)
        {
            var createdCompany = CreatedCompany ?? throw new InvalidOperationException("Company was not created.");
            return Task.FromResult(new Company(
                companyId,
                createdCompany.CompanyUuid,
                createdCompany.LegalName,
                createdCompany.TradeName,
                createdCompany.Gstin,
                createdCompany.ContactEmailAddress,
                createdCompany.ContactPhoneNumber,
                createdCompany.AddressLine1,
                createdCompany.AddressLine2,
                createdCompany.City,
                createdCompany.State,
                createdCompany.PostalCode,
                createdCompany.Country,
                createdCompany.Status,
                IsDeleted: false,
                DateTime.UtcNow));
        }

        public Task<Company> GetByUuidAsync(Guid companyUuid, CancellationToken cancellationToken)
        {
            var createdCompany = CreatedCompany ?? throw new InvalidOperationException("Company was not created.");
            return Task.FromResult(new Company(
                100,
                companyUuid,
                createdCompany.LegalName,
                createdCompany.TradeName,
                createdCompany.Gstin,
                createdCompany.ContactEmailAddress,
                createdCompany.ContactPhoneNumber,
                createdCompany.AddressLine1,
                createdCompany.AddressLine2,
                createdCompany.City,
                createdCompany.State,
                createdCompany.PostalCode,
                createdCompany.Country,
                createdCompany.Status,
                IsDeleted: false,
                DateTime.UtcNow));
        }
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
}

using Microsoft.AspNetCore.Http;
using RMS.Identity.Service.Api.Shared.Auth;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Entities.Companies;
using RMS.Identity.Service.Domain.Entities.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.UserAccounts;

namespace RMS.Identity.Service.Tests.Shared.Auth;

public sealed class CompanyAccessAuthorizerTests
{
    [Fact]
    public async Task AuthorizeMembershipAsync_WithActiveMembership_ReturnsMembership()
    {
        var userUuid = Guid.NewGuid();
        var companyUuid = Guid.NewGuid();
        var membership = new CompanyMembership(userUuid, companyUuid, "verified", "OWNER", "active");
        var authorizer = new CompanyAccessAuthorizer(
            new StubCompanyMembershipReadRepository(membership),
            new StubUserAccountReadRepository(CreateUser(userUuid)));

        var result = await authorizer.AuthorizeMembershipAsync(userUuid, companyUuid, CancellationToken.None);

        Assert.Equal(membership, result);
    }

    [Fact]
    public async Task AuthorizeMembershipAsync_WithoutActiveMembership_ThrowsForbidden()
    {
        var userUuid = Guid.NewGuid();
        var authorizer = new CompanyAccessAuthorizer(
            new StubCompanyMembershipReadRepository(null),
            new StubUserAccountReadRepository(CreateUser(userUuid)));

        var exception = await Assert.ThrowsAsync<ServiceException>(() =>
            authorizer.AuthorizeMembershipAsync(userUuid, Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(StatusCodes.Status403Forbidden, exception.StatusCode);
        Assert.Equal("COMPANY_ACCESS_DENIED", exception.Code);
    }

    [Fact]
    public async Task AuthorizeMembershipAsync_WithInactiveUser_ThrowsForbidden()
    {
        var userUuid = Guid.NewGuid();
        var companyUuid = Guid.NewGuid();
        var membership = new CompanyMembership(userUuid, companyUuid, "verified", "OWNER", "active");
        var authorizer = new CompanyAccessAuthorizer(
            new StubCompanyMembershipReadRepository(membership),
            new StubUserAccountReadRepository(CreateUser(userUuid, isActive: false)));

        var exception = await Assert.ThrowsAsync<ServiceException>(() =>
            authorizer.AuthorizeMembershipAsync(userUuid, companyUuid, CancellationToken.None));

        Assert.Equal(StatusCodes.Status403Forbidden, exception.StatusCode);
        Assert.Equal("USER_NOT_ACTIVE", exception.Code);
    }

    [Fact]
    public async Task AuthorizeRoleAsync_WithMissingRole_ThrowsForbidden()
    {
        var userUuid = Guid.NewGuid();
        var companyUuid = Guid.NewGuid();
        var membership = new CompanyMembership(userUuid, companyUuid, "verified", "MEMBER", "active");
        var authorizer = new CompanyAccessAuthorizer(
            new StubCompanyMembershipReadRepository(membership),
            new StubUserAccountReadRepository(CreateUser(userUuid)));

        var exception = await Assert.ThrowsAsync<ServiceException>(() =>
            authorizer.AuthorizeRoleAsync(userUuid, companyUuid, ["OWNER", "ADMIN"], CancellationToken.None));

        Assert.Equal(StatusCodes.Status403Forbidden, exception.StatusCode);
        Assert.Equal("COMPANY_ROLE_REQUIRED", exception.Code);
    }

    [Theory]
    [InlineData("rejected")]
    [InlineData("suspended")]
    public async Task AuthorizeMembershipAsync_WithUnavailableCompanyStatus_ThrowsForbidden(string companyStatus)
    {
        var userUuid = Guid.NewGuid();
        var companyUuid = Guid.NewGuid();
        var membership = new CompanyMembership(userUuid, companyUuid, companyStatus, "OWNER", "active");
        var authorizer = new CompanyAccessAuthorizer(
            new StubCompanyMembershipReadRepository(membership),
            new StubUserAccountReadRepository(CreateUser(userUuid)));

        var exception = await Assert.ThrowsAsync<ServiceException>(() =>
            authorizer.AuthorizeMembershipAsync(userUuid, companyUuid, CancellationToken.None));

        Assert.Equal(StatusCodes.Status403Forbidden, exception.StatusCode);
        Assert.Equal("COMPANY_ACCESS_DENIED", exception.Code);
    }

    private static UserAccount CreateUser(Guid userUuid, bool isActive = true) =>
        new(
            10,
            userUuid,
            "owner@example.com",
            "Owner",
            EmailVerified: true,
            IsActive: isActive,
            IsDeleted: false,
            DateTime.UtcNow);

    private sealed class StubCompanyMembershipReadRepository : ICompanyMembershipReadRepository
    {
        private readonly CompanyMembership? _membership;

        public StubCompanyMembershipReadRepository(CompanyMembership? membership)
        {
            _membership = membership;
        }

        public Task<IReadOnlyCollection<UserCompanyMembership>> ListByUserUuidAsync(
            Guid userUuid,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<UserCompanyMembership>>(Array.Empty<UserCompanyMembership>());

        public Task<CompanyMembership?> GetMembershipAsync(
            Guid userUuid,
            Guid companyUuid,
            CancellationToken cancellationToken) =>
            Task.FromResult(_membership);
    }

    private sealed class StubUserAccountReadRepository : IUserAccountReadRepository
    {
        private readonly UserAccount _user;

        public StubUserAccountReadRepository(UserAccount user)
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
}

using Microsoft.AspNetCore.Http;
using RMS.Identity.Service.Api.Shared.Auth;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Entities.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Companies;

namespace RMS.Identity.Service.Tests.Shared.Auth;

public sealed class CompanyAccessAuthorizerTests
{
    [Fact]
    public async Task AuthorizeMembershipAsync_WithActiveMembership_ReturnsMembership()
    {
        var userUuid = Guid.NewGuid();
        var companyUuid = Guid.NewGuid();
        var membership = new CompanyMembership(userUuid, companyUuid, "OWNER", "active");
        var authorizer = new CompanyAccessAuthorizer(new StubCompanyMembershipReadRepository(membership));

        var result = await authorizer.AuthorizeMembershipAsync(userUuid, companyUuid, CancellationToken.None);

        Assert.Equal(membership, result);
    }

    [Fact]
    public async Task AuthorizeMembershipAsync_WithoutActiveMembership_ThrowsForbidden()
    {
        var authorizer = new CompanyAccessAuthorizer(new StubCompanyMembershipReadRepository(null));

        var exception = await Assert.ThrowsAsync<ServiceException>(() =>
            authorizer.AuthorizeMembershipAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(StatusCodes.Status403Forbidden, exception.StatusCode);
        Assert.Equal("COMPANY_ACCESS_DENIED", exception.Code);
    }

    [Fact]
    public async Task AuthorizeRoleAsync_WithMissingRole_ThrowsForbidden()
    {
        var userUuid = Guid.NewGuid();
        var companyUuid = Guid.NewGuid();
        var membership = new CompanyMembership(userUuid, companyUuid, "MEMBER", "active");
        var authorizer = new CompanyAccessAuthorizer(new StubCompanyMembershipReadRepository(membership));

        var exception = await Assert.ThrowsAsync<ServiceException>(() =>
            authorizer.AuthorizeRoleAsync(userUuid, companyUuid, ["OWNER", "ADMIN"], CancellationToken.None));

        Assert.Equal(StatusCodes.Status403Forbidden, exception.StatusCode);
        Assert.Equal("COMPANY_ROLE_REQUIRED", exception.Code);
    }

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
}

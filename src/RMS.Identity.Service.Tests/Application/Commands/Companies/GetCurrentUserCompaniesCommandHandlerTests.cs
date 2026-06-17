using Microsoft.AspNetCore.Http;
using RMS.Identity.Service.Application.Commands.Companies;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.Companies;
using RMS.Identity.Service.Domain.Entities.Companies;
using RMS.Identity.Service.Domain.Entities.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.UserAccounts;

namespace RMS.Identity.Service.Tests.Application.Commands.Companies;

public sealed class GetCurrentUserCompaniesCommandHandlerTests
{
    private static readonly Guid UserUuid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CompanyUuid = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task HandleAsync_WithActiveUser_ReturnsCurrentUserCompanies()
    {
        var handler = new GetCurrentUserCompaniesCommandHandler(
            new FakeCompanyMembershipReadRepository(new[]
            {
                CreateMembership()
            }),
            new FakeUserAccountReadRepository(CreateUser(isActive: true, isDeleted: false)));

        var response = await handler.HandleAsync(
            new GetCurrentUserCompaniesCommandRequest(UserUuid),
            CancellationToken.None);

        var company = Assert.Single(response.Companies);
        Assert.Equal(CompanyUuid, company.CompanyUuid);
        Assert.Equal("Example Retail Pvt Ltd", company.LegalName);
        Assert.Equal("OWNER", company.CompanyRole);
        Assert.Equal("active", company.MembershipStatus);
    }

    [Fact]
    public async Task HandleAsync_WithInactiveUser_ThrowsForbidden()
    {
        var handler = new GetCurrentUserCompaniesCommandHandler(
            new FakeCompanyMembershipReadRepository(new[] { CreateMembership() }),
            new FakeUserAccountReadRepository(CreateUser(isActive: false, isDeleted: false)));

        var exception = await Assert.ThrowsAnyAsync<ServiceException>(() =>
            handler.HandleAsync(
                new GetCurrentUserCompaniesCommandRequest(UserUuid),
                CancellationToken.None));

        Assert.Equal(StatusCodes.Status403Forbidden, exception.StatusCode);
        Assert.Equal("403", exception.Code);
    }

    private static UserCompanyMembership CreateMembership() =>
        new(
            CompanyUuid,
            "Example Retail Pvt Ltd",
            "Example Retail",
            "29ABCDE1234F1Z5",
            "pending_verification",
            "OWNER",
            "active",
            DateTime.UtcNow);

    private static UserAccount CreateUser(bool isActive, bool isDeleted) =>
        new(
            10,
            UserUuid,
            "owner@example.com",
            "Owner",
            EmailVerified: true,
            IsActive: isActive,
            IsDeleted: isDeleted,
            DateTime.UtcNow);

    private sealed class FakeCompanyMembershipReadRepository : ICompanyMembershipReadRepository
    {
        private readonly IReadOnlyCollection<UserCompanyMembership> _memberships;

        public FakeCompanyMembershipReadRepository(IReadOnlyCollection<UserCompanyMembership> memberships)
        {
            _memberships = memberships;
        }

        public Task<IReadOnlyCollection<UserCompanyMembership>> ListByUserUuidAsync(
            Guid userUuid,
            CancellationToken cancellationToken) =>
            Task.FromResult(_memberships);

        public Task<CompanyMembership?> GetMembershipAsync(
            Guid userUuid,
            Guid companyUuid,
            CancellationToken cancellationToken) =>
            Task.FromResult<CompanyMembership?>(null);
    }

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
}

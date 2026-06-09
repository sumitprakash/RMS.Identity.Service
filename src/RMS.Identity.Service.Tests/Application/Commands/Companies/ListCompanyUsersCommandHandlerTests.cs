using RMS.Identity.Service.Application.Commands.Companies;
using RMS.Identity.Service.Domain.Contracts.CompanyUsers;
using RMS.Identity.Service.Domain.Entities.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.CompanyUsers;

namespace RMS.Identity.Service.Tests.Application.Commands.Companies;

public sealed class ListCompanyUsersCommandHandlerTests
{
    private static readonly Guid CompanyUuid = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task HandleAsync_ReturnsCompanyUsersWithStatuses()
    {
        var handler = new ListCompanyUsersCommandHandler(new FakeCompanyUserReadRepository(new[]
        {
            CreateUser(Guid.NewGuid(), "owner@example.com", "OWNER", "active", emailVerified: true),
            CreateUser(Guid.NewGuid(), "cashier@example.com", "MEMBER", "suspended", emailVerified: true)
        }));

        var response = await handler.HandleAsync(
            new ListCompanyUsersCommandRequest(CompanyUuid),
            CancellationToken.None);

        Assert.Equal(2, response.Users.Count);
        Assert.Contains(response.Users, user => user.Username == "owner@example.com" && user.Status == "active");
        Assert.Contains(response.Users, user => user.Username == "cashier@example.com" && user.Status == "suspended");
    }

    private static CompanyUserAccount CreateUser(
        Guid userUuid,
        string username,
        string companyRole,
        string membershipStatus,
        bool emailVerified) =>
        new(
            userUuid,
            username,
            "User",
            emailVerified,
            IsActive: true,
            companyRole,
            membershipStatus,
            DateTime.UtcNow);

    private sealed class FakeCompanyUserReadRepository : ICompanyUserReadRepository
    {
        private readonly IReadOnlyCollection<CompanyUserAccount> _users;

        public FakeCompanyUserReadRepository(IReadOnlyCollection<CompanyUserAccount> users)
        {
            _users = users;
        }

        public Task<IReadOnlyCollection<CompanyUserAccount>> ListByCompanyUuidAsync(
            Guid companyUuid,
            CancellationToken cancellationToken) =>
            Task.FromResult(_users);

        public Task<CompanyUserAccount?> GetByCompanyAndUserUuidAsync(
            Guid companyUuid,
            Guid userUuid,
            CancellationToken cancellationToken) =>
            Task.FromResult(_users.FirstOrDefault(user => user.UserUuid == userUuid));

        public Task<int> CountActiveOwnersAsync(Guid companyUuid, CancellationToken cancellationToken) =>
            Task.FromResult(_users.Count(user =>
                user.CompanyRole == "OWNER"
                && user.MembershipStatus == "active"
                && user.IsActive));
    }
}

using System.Net;
using RMS.Identity.Service.Api.Shared.Auth;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Entities.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Roles;
using RMS.Identity.Service.Domain.Interfaces.Repositories.UserAccounts;

namespace RMS.Identity.Service.Tests.Api.Shared.Auth;

public sealed class PlatformAdminAuthorizerTests
{
    private static readonly Guid UserUuid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task AuthorizeAsync_WithPlatformAdminRole_AllowsAccess()
    {
        var authorizer = new PlatformAdminAuthorizer(
            new FakeOperationalRoleReadRepository(hasRole: true),
            new FakeUserAccountReadRepository(CreateUser()));

        await authorizer.AuthorizeAsync(UserUuid, CancellationToken.None);
    }

    [Fact]
    public async Task AuthorizeAsync_WithoutPlatformAdminRole_ThrowsForbidden()
    {
        var authorizer = new PlatformAdminAuthorizer(
            new FakeOperationalRoleReadRepository(hasRole: false),
            new FakeUserAccountReadRepository(CreateUser()));

        var exception = await Assert.ThrowsAsync<ServiceException>(() =>
            authorizer.AuthorizeAsync(UserUuid, CancellationToken.None));

        Assert.Equal((int)HttpStatusCode.Forbidden, exception.StatusCode);
        Assert.Equal("PLATFORM_ADMIN_REQUIRED", exception.Code);
    }

    private static UserAccount CreateUser(bool isActive = true) =>
        new(
            10,
            UserUuid,
            "admin@example.com",
            "Platform Admin",
            EmailVerified: true,
            isActive,
            IsDeleted: false,
            DateTime.UtcNow);

    private sealed class FakeOperationalRoleReadRepository : IOperationalRoleReadRepository
    {
        private readonly bool _hasRole;

        public FakeOperationalRoleReadRepository(bool hasRole)
        {
            _hasRole = hasRole;
        }

        public Task<bool> UserHasAnyRoleAsync(
            Guid userUuid,
            IReadOnlyCollection<string> roleNames,
            CancellationToken cancellationToken) =>
            Task.FromResult(_hasRole && roleNames.Contains("PLATFORM_ADMIN"));
    }

    private sealed class FakeUserAccountReadRepository : IUserAccountReadRepository
    {
        private readonly UserAccount _user;

        public FakeUserAccountReadRepository(UserAccount user)
        {
            _user = user;
        }

        public Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<UserAccount> GetByIdAsync(long userId, CancellationToken cancellationToken) =>
            Task.FromResult(_user);

        public Task<UserAccount> GetByUuidAsync(Guid userUuid, CancellationToken cancellationToken) =>
            Task.FromResult(_user);
    }
}

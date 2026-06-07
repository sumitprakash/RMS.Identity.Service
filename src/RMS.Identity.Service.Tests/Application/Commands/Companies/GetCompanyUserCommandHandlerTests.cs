using System.Net;
using RMS.Identity.Service.Application.Commands.Companies;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.CompanyUsers;
using RMS.Identity.Service.Domain.Entities.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.CompanyUsers;

namespace RMS.Identity.Service.Tests.Application.Commands.Companies;

public sealed class GetCompanyUserCommandHandlerTests
{
    private static readonly Guid CompanyUuid = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid UserUuid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task HandleAsync_WithActiveVerifiedCompanyUser_ReturnsActiveUserResponse()
    {
        var createdAt = DateTime.UtcNow;
        var handler = new GetCompanyUserCommandHandler(new FakeCompanyUserReadRepository(
            new CompanyUserAccount(
                UserUuid,
                "cashier@example.com",
                "Store Cashier",
                EmailVerified: true,
                IsActive: true,
                "MEMBER",
                "active",
                createdAt)));

        var response = await handler.HandleAsync(
            new GetCompanyUserCommandRequest(CompanyUuid, UserUuid),
            CancellationToken.None);

        Assert.Equal(UserUuid, response.UserUuid);
        Assert.Equal("cashier@example.com", response.Username);
        Assert.Equal("Store Cashier", response.DisplayName);
        Assert.Empty(response.Roles);
        Assert.Equal("MEMBER", response.CompanyRole);
        Assert.Equal("active", response.Status);
        Assert.Equal(createdAt, response.CreatedAt);
    }

    [Fact]
    public async Task HandleAsync_WithUnverifiedCompanyUser_ReturnsPendingStatus()
    {
        var handler = new GetCompanyUserCommandHandler(new FakeCompanyUserReadRepository(
            CreateUser(emailVerified: false, isActive: true, membershipStatus: "active")));

        var response = await handler.HandleAsync(
            new GetCompanyUserCommandRequest(CompanyUuid, UserUuid),
            CancellationToken.None);

        Assert.Equal("pending", response.Status);
    }

    [Fact]
    public async Task HandleAsync_WithSuspendedMembership_ReturnsSuspendedStatus()
    {
        var handler = new GetCompanyUserCommandHandler(new FakeCompanyUserReadRepository(
            CreateUser(emailVerified: true, isActive: true, membershipStatus: "suspended")));

        var response = await handler.HandleAsync(
            new GetCompanyUserCommandRequest(CompanyUuid, UserUuid),
            CancellationToken.None);

        Assert.Equal("suspended", response.Status);
    }

    [Fact]
    public async Task HandleAsync_WithMissingCompanyUser_ThrowsNotFound()
    {
        var handler = new GetCompanyUserCommandHandler(new FakeCompanyUserReadRepository(null));

        var exception = await Assert.ThrowsAsync<ServiceException>(() =>
            handler.HandleAsync(
                new GetCompanyUserCommandRequest(CompanyUuid, UserUuid),
                CancellationToken.None));

        Assert.Equal((int)HttpStatusCode.NotFound, exception.StatusCode);
        Assert.Equal("COMPANY_USER_NOT_FOUND", exception.Code);
    }

    private static CompanyUserAccount CreateUser(
        bool emailVerified,
        bool isActive,
        string membershipStatus) =>
        new(
            UserUuid,
            "cashier@example.com",
            "Store Cashier",
            emailVerified,
            isActive,
            "MEMBER",
            membershipStatus,
            DateTime.UtcNow);

    private sealed class FakeCompanyUserReadRepository : ICompanyUserReadRepository
    {
        private readonly CompanyUserAccount? _user;

        public FakeCompanyUserReadRepository(CompanyUserAccount? user)
        {
            _user = user;
        }

        public Task<CompanyUserAccount?> GetByCompanyAndUserUuidAsync(
            Guid companyUuid,
            Guid userUuid,
            CancellationToken cancellationToken) =>
            Task.FromResult(_user);
    }
}

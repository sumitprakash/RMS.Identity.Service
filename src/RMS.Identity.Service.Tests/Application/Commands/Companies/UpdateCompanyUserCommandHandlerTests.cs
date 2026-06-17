using System.Net;
using RMS.Identity.Service.Application.Commands.Companies;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.CompanyUsers;
using RMS.Identity.Service.Domain.Entities.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.CompanyUsers;

namespace RMS.Identity.Service.Tests.Application.Commands.Companies;

public sealed class UpdateCompanyUserCommandHandlerTests
{
    private static readonly Guid ActorUserUuid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CompanyUuid = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid UserUuid = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task HandleAsync_WithValidRequest_UpdatesMembership()
    {
        var repository = new FakeCompanyUserRepository(CreateUser("MEMBER", "active"), activeOwnerCount: 1);
        var handler = new UpdateCompanyUserCommandHandler(repository, repository);

        var response = await handler.HandleAsync(
            new UpdateCompanyUserCommandRequest(ActorUserUuid, CompanyUuid, UserUuid, "admin", "active"),
            CancellationToken.None);

        Assert.Equal("ADMIN", response.CompanyRole);
        Assert.Equal("active", response.Status);
        Assert.Equal("ADMIN", repository.UpdatedMembership?.CompanyRole);
        Assert.Equal("active", repository.UpdatedMembership?.MembershipStatus);
    }

    [Fact]
    public async Task HandleAsync_WhenSuspendingLastOwner_ThrowsConflict()
    {
        var repository = new FakeCompanyUserRepository(CreateUser("OWNER", "active"), activeOwnerCount: 1);
        var handler = new UpdateCompanyUserCommandHandler(repository, repository);

        var exception = await Assert.ThrowsAnyAsync<ServiceException>(() =>
            handler.HandleAsync(
                new UpdateCompanyUserCommandRequest(ActorUserUuid, CompanyUuid, UserUuid, "OWNER", "suspended"),
                CancellationToken.None));

        Assert.Equal((int)HttpStatusCode.Conflict, exception.StatusCode);
        Assert.Equal("409", exception.Code);
        Assert.Null(repository.UpdatedMembership);
    }

    private static CompanyUserAccount CreateUser(string companyRole, string membershipStatus) =>
        new(
            UserUuid,
            "owner@example.com",
            "Owner",
            EmailVerified: true,
            IsActive: true,
            companyRole,
            membershipStatus,
            DateTime.UtcNow);

    private sealed class FakeCompanyUserRepository : ICompanyUserReadRepository, ICompanyUserWriteRepository
    {
        private CompanyUserAccount _user;
        private readonly int _activeOwnerCount;

        public FakeCompanyUserRepository(CompanyUserAccount user, int activeOwnerCount)
        {
            _user = user;
            _activeOwnerCount = activeOwnerCount;
        }

        public UpdateCompanyUserCommand? UpdatedMembership { get; private set; }

        public Task<IReadOnlyCollection<CompanyUserAccount>> ListByCompanyUuidAsync(
            Guid companyUuid,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<CompanyUserAccount>>(new[] { _user });

        public Task<CompanyUserAccount?> GetByCompanyAndUserUuidAsync(
            Guid companyUuid,
            Guid userUuid,
            CancellationToken cancellationToken) =>
            Task.FromResult<CompanyUserAccount?>(_user);

        public Task<int> CountActiveOwnersAsync(Guid companyUuid, CancellationToken cancellationToken) =>
            Task.FromResult(_activeOwnerCount);

        public Task CreateAsync(CreateCompanyUserCommand command, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<int> CountActiveOwnersForUpdateAsync(Guid companyUuid, CancellationToken cancellationToken) =>
            Task.FromResult(_activeOwnerCount);

        public Task UpdateMembershipAsync(UpdateCompanyUserCommand command, CancellationToken cancellationToken)
        {
            UpdatedMembership = command;
            _user = _user with
            {
                CompanyRole = command.CompanyRole,
                MembershipStatus = command.MembershipStatus
            };
            return Task.CompletedTask;
        }
    }
}

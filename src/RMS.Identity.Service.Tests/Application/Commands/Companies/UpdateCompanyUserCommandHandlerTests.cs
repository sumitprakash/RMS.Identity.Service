using System.Net;
using RMS.Identity.Service.Application.Commands.Companies;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.CompanyUsers;
using RMS.Identity.Service.Domain.Entities.Companies;
using RMS.Identity.Service.Domain.Entities.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Repositories.AuditLog;
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
        var auditRepository = new FakeAuditLogWriteRepository();
        var handler = new UpdateCompanyUserCommandHandler(repository, repository, auditRepository);

        var response = await handler.HandleAsync(
            new UpdateCompanyUserCommandRequest(
                ActorUserUuid,
                CompanyUuid,
                UserUuid,
                CompanyRole.Admin,
                CompanyMembershipStatus.Active),
            CancellationToken.None);

        Assert.Equal("ADMIN", response.CompanyRole);
        Assert.Equal("active", response.Status);
        Assert.Equal("ADMIN", repository.UpdatedMembership?.CompanyRole);
        Assert.Equal("active", repository.UpdatedMembership?.MembershipStatus);
        Assert.Equal("company_user_membership_updated", auditRepository.Action);
        Assert.Equal(ActorUserUuid, auditRepository.ActorUserUuid);
        Assert.Equal("MEMBER", auditRepository.PreviousCompanyRole);
    }

    [Fact]
    public async Task HandleAsync_WhenSuspendingLastOwner_ThrowsConflict()
    {
        var repository = new FakeCompanyUserRepository(CreateUser("OWNER", "active"), activeOwnerCount: 1);
        var handler = new UpdateCompanyUserCommandHandler(
            repository,
            repository,
            new FakeAuditLogWriteRepository());

        var exception = await Assert.ThrowsAnyAsync<ServiceException>(() =>
            handler.HandleAsync(
                new UpdateCompanyUserCommandRequest(
                    ActorUserUuid,
                    CompanyUuid,
                    UserUuid,
                    CompanyRole.Owner,
                    CompanyMembershipStatus.Suspended),
                CancellationToken.None));

        Assert.Equal((int)HttpStatusCode.Conflict, exception.StatusCode);
        Assert.Equal("409-5-2", exception.Code);
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

    private sealed class FakeAuditLogWriteRepository : IAuditLogWriteRepository
    {
        public string? Action { get; private set; }

        public Guid? ActorUserUuid { get; private set; }

        public string? PreviousCompanyRole { get; private set; }

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
            PreviousCompanyRole = previousCompanyRole;
            return Task.CompletedTask;
        }
    }
}

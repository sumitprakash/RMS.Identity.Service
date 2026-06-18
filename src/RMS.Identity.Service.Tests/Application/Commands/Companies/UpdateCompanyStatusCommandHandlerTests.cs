using System.Net;
using RMS.Identity.Service.Application.Commands.Companies;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.Companies;
using RMS.Identity.Service.Domain.Entities.Companies;
using RMS.Identity.Service.Domain.Entities.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Repositories.AuditLog;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.UserAccounts;

namespace RMS.Identity.Service.Tests.Application.Commands.Companies;

public sealed class UpdateCompanyStatusCommandHandlerTests
{
    private static readonly Guid ActorUserUuid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CompanyUuid = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Theory]
    [InlineData("pending_verification", "verified")]
    [InlineData("pending_verification", "rejected")]
    [InlineData("verified", "suspended")]
    [InlineData("suspended", "verified")]
    public async Task HandleAsync_WithValidTransition_UpdatesStatusAndAudits(
        string currentStatus,
        string targetStatus)
    {
        var companyRepository = new FakeCompanyRepository(CreateCompany(currentStatus));
        var auditRepository = new FakeAuditLogWriteRepository();
        var handler = new UpdateCompanyStatusCommandHandler(
            auditRepository,
            companyRepository,
            companyRepository,
            new FakeUserAccountReadRepository());

        var response = await handler.HandleAsync(
            new UpdateCompanyStatusCommandRequest(ActorUserUuid, CompanyUuid, targetStatus),
            CancellationToken.None);

        Assert.Equal(targetStatus, response.Status);
        Assert.Equal(targetStatus, companyRepository.UpdatedStatus?.Status);
        Assert.Equal(currentStatus, auditRepository.PreviousStatus);
        Assert.Equal(targetStatus, auditRepository.Company?.Status);
        Assert.Equal(10, auditRepository.ActorUserId);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidTransition_ThrowsConflict()
    {
        var companyRepository = new FakeCompanyRepository(CreateCompany("rejected"));
        var auditRepository = new FakeAuditLogWriteRepository();
        var handler = new UpdateCompanyStatusCommandHandler(
            auditRepository,
            companyRepository,
            companyRepository,
            new FakeUserAccountReadRepository());

        var exception = await Assert.ThrowsAnyAsync<ServiceException>(() =>
            handler.HandleAsync(
                new UpdateCompanyStatusCommandRequest(ActorUserUuid, CompanyUuid, "verified"),
                CancellationToken.None));

        Assert.Equal((int)HttpStatusCode.Conflict, exception.StatusCode);
        Assert.Equal("409-4-3", exception.Code);
        Assert.Null(companyRepository.UpdatedStatus);
        Assert.Null(auditRepository.Company);
    }

    private static Company CreateCompany(string status) =>
        new(
            100,
            CompanyUuid,
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
            status,
            IsDeleted: false,
            DateTime.UtcNow);

    private sealed class FakeCompanyRepository : ICompanyReadRepository, ICompanyWriteRepository
    {
        private Company _company;

        public FakeCompanyRepository(Company company)
        {
            _company = company;
        }

        public UpdateCompanyStatusCommand? UpdatedStatus { get; private set; }

        public Task<bool> ExistsByGstinAsync(string gstin, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<long> CreateAsync(CreateCompanyCommand command, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task UpdateAsync(UpdateCompanyCommand command, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task UpdateStatusAsync(UpdateCompanyStatusCommand command, CancellationToken cancellationToken)
        {
            UpdatedStatus = command;
            _company = _company with { Status = command.Status };
            return Task.CompletedTask;
        }

        public Task<Company> GetByIdAsync(long companyId, CancellationToken cancellationToken) =>
            Task.FromResult(_company);

        public Task<Company> GetByUuidAsync(Guid companyUuid, CancellationToken cancellationToken) =>
            Task.FromResult(_company);
    }

    private sealed class FakeAuditLogWriteRepository : IAuditLogWriteRepository
    {
        public Company? Company { get; private set; }

        public string? PreviousStatus { get; private set; }

        public long? ActorUserId { get; private set; }

        public Task InsertSignUpCreatedAsync(UserAccount account, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task InsertCompanyStatusChangedAsync(
            Company company,
            string previousStatus,
            long actorUserId,
            CancellationToken cancellationToken)
        {
            Company = company;
            PreviousStatus = previousStatus;
            ActorUserId = actorUserId;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserAccountReadRepository : IUserAccountReadRepository
    {
        public Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<UserAccount> GetByIdAsync(long userId, CancellationToken cancellationToken) =>
            Task.FromResult(CreateUser());

        public Task<UserAccount> GetByUuidAsync(Guid userUuid, CancellationToken cancellationToken) =>
            Task.FromResult(CreateUser());

        private static UserAccount CreateUser() =>
            new(
                10,
                ActorUserUuid,
                "admin@example.com",
                "Platform Admin",
                EmailVerified: true,
                IsActive: true,
                IsDeleted: false,
                DateTime.UtcNow);
    }
}

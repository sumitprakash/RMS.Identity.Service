using RMS.Identity.Service.Application.Commands.Companies;
using RMS.Identity.Service.Domain.Contracts.Companies;
using RMS.Identity.Service.Domain.Entities.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Companies;

namespace RMS.Identity.Service.Tests.Application.Commands.Companies;

public sealed class UpdateCompanyCommandHandlerTests
{
    private static readonly Guid CompanyUuid = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task HandleAsync_WithValidRequest_UpdatesCompanyMetadata()
    {
        var repository = new FakeCompanyRepository(CreateCompany(
            "Existing Retail Pvt Ltd",
            "Existing Retail",
            "29ABCDE1234F1Z5",
            "accounts@example.com"));
        var handler = new UpdateCompanyCommandHandler(repository, repository);

        var response = await handler.HandleAsync(
            new UpdateCompanyCommandRequest(
                CompanyUuid,
                " Updated Retail Pvt Ltd ",
                " Updated Retail ",
                "29abcde5678f1z5",
                " Billing@Example.com ",
                " +919876543211 ",
                " 2 Main Road ",
                " Near Market ",
                " Bengaluru ",
                " Karnataka ",
                " 560002 ",
                " in "),
            CancellationToken.None);

        Assert.Equal("Updated Retail Pvt Ltd", response.LegalName);
        Assert.Equal("Updated Retail", response.TradeName);
        Assert.Equal("29ABCDE5678F1Z5", response.Gstin);
        Assert.Equal("billing@example.com", response.ContactEmailAddress);
        Assert.Equal("2 Main Road", response.AddressLine1);
        Assert.Equal("pending_verification", response.Status);
        Assert.Equal("29ABCDE5678F1Z5", repository.UpdatedCompany?.Gstin);
    }

    private static Company CreateCompany(
        string legalName,
        string? tradeName,
        string gstin,
        string contactEmailAddress) =>
        new(
            100,
            CompanyUuid,
            legalName,
            tradeName,
            gstin,
            contactEmailAddress,
            "+919876543211",
            "1 Main Road",
            null,
            "Bengaluru",
            "Karnataka",
            "560001",
            "IN",
            "pending_verification",
            IsDeleted: false,
            DateTime.UtcNow);

    private sealed class FakeCompanyRepository : ICompanyReadRepository, ICompanyWriteRepository
    {
        private Company _company;

        public FakeCompanyRepository(Company company)
        {
            _company = company;
        }

        public UpdateCompanyCommand? UpdatedCompany { get; private set; }

        public Task<bool> ExistsByGstinAsync(string gstin, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<long> CreateAsync(CreateCompanyCommand command, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task UpdateAsync(UpdateCompanyCommand command, CancellationToken cancellationToken)
        {
            UpdatedCompany = command;
            _company = _company with
            {
                LegalName = command.LegalName,
                TradeName = command.TradeName,
                Gstin = command.Gstin,
                ContactEmailAddress = command.ContactEmailAddress,
                ContactPhoneNumber = command.ContactPhoneNumber,
                AddressLine1 = command.AddressLine1,
                AddressLine2 = command.AddressLine2,
                City = command.City,
                State = command.State,
                PostalCode = command.PostalCode,
                Country = command.Country
            };
            return Task.CompletedTask;
        }

        public Task UpdateStatusAsync(UpdateCompanyStatusCommand command, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Company> GetByIdAsync(long companyId, CancellationToken cancellationToken) =>
            Task.FromResult(_company);

        public Task<Company> GetByUuidAsync(Guid companyUuid, CancellationToken cancellationToken) =>
            Task.FromResult(_company);
    }
}

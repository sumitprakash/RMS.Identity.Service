using RMS.Identity.Service.Application.Commands.Companies;
using RMS.Identity.Service.Domain.Contracts.Companies;
using RMS.Identity.Service.Domain.Entities.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Companies;

namespace RMS.Identity.Service.Tests.Application.Commands.Companies;

public sealed class GetCompanyCommandHandlerTests
{
    private static readonly Guid CompanyUuid = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task HandleAsync_WithExistingCompany_ReturnsCompanyMetadata()
    {
        var repository = new FakeCompanyReadRepository();
        var handler = new GetCompanyCommandHandler(repository);

        var response = await handler.HandleAsync(
            new GetCompanyCommandRequest(CompanyUuid),
            CancellationToken.None);

        Assert.Equal(CompanyUuid, response.CompanyUuid);
        Assert.Null(response.CompanyCode);
        Assert.Equal("Example Retail Pvt Ltd", response.LegalName);
        Assert.Equal("Example Retail", response.TradeName);
        Assert.Equal("29ABCDE1234F1Z5", response.Gstin);
        Assert.Equal("accounts@example.com", response.ContactEmailAddress);
        Assert.Equal("+919876543211", response.ContactPhoneNumber);
        Assert.Equal("1 Main Road", response.AddressLine1);
        Assert.Equal("Near Market", response.AddressLine2);
        Assert.Equal("Bengaluru", response.City);
        Assert.Equal("Karnataka", response.State);
        Assert.Equal("560001", response.PostalCode);
        Assert.Equal("IN", response.Country);
        Assert.Equal("pending_verification", response.Status);
    }

    private sealed class FakeCompanyReadRepository : ICompanyReadRepository
    {
        public Task<bool> ExistsByGstinAsync(string gstin, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<Company> GetByIdAsync(long companyId, CancellationToken cancellationToken) =>
            Task.FromResult(CreateCompany());

        public Task<Company> GetByUuidAsync(Guid companyUuid, CancellationToken cancellationToken) =>
            Task.FromResult(CreateCompany() with { CompanyUuid = companyUuid });

        private static Company CreateCompany() =>
            new(
                100,
                CompanyUuid,
                "Example Retail Pvt Ltd",
                "Example Retail",
                "29ABCDE1234F1Z5",
                "accounts@example.com",
                "+919876543211",
                "1 Main Road",
                "Near Market",
                "Bengaluru",
                "Karnataka",
                "560001",
                "IN",
                "pending_verification",
                IsDeleted: false,
                DateTime.UtcNow);
    }
}

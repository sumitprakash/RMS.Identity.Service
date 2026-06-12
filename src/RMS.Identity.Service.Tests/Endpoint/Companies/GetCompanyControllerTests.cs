using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RMS.Identity.Service.Api.Endpoint.Companies.GetCompany;
using RMS.Identity.Service.Api.Shared.Auth;
using RMS.Identity.Service.Domain.Contracts.Companies;
using RMS.Identity.Service.Domain.Entities.Companies;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Tests.Endpoint.Companies;

public sealed class GetCompanyControllerTests
{
    [Fact]
    public async Task GetAsync_RunsReadFlowInsideDatabaseTransaction()
    {
        var userUuid = Guid.NewGuid();
        var companyUuid = Guid.NewGuid();
        var transactionExecutor = new StubDatabaseTransactionExecutor();
        var controller = new GetCompanyController(
            new StubAccessTokenUserResolver(userUuid),
            new StubCompanyAccessAuthorizer(),
            transactionExecutor,
            new StubGetCompanyCommandHandler())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.GetAsync(companyUuid, CancellationToken.None);

        Assert.True(transactionExecutor.Executed);
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<CompanyResponse>(ok.Value);
        Assert.Equal(companyUuid, response.CompanyUuid);
    }

    private sealed class StubDatabaseTransactionExecutor : IDatabaseTransactionExecutor
    {
        public bool Executed { get; private set; }

        public async Task<TResult> ExecuteAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken)
        {
            Executed = true;
            return await operation(cancellationToken);
        }
    }

    private sealed class StubAccessTokenUserResolver : IAccessTokenUserResolver
    {
        private readonly Guid _userUuid;

        public StubAccessTokenUserResolver(Guid userUuid)
        {
            _userUuid = userUuid;
        }

        public Guid ResolveRequiredUserUuid(HttpContext context) => _userUuid;
    }

    private sealed class StubCompanyAccessAuthorizer : ICompanyAccessAuthorizer
    {
        public Task<CompanyMembership> AuthorizeMembershipAsync(
            Guid userUuid,
            Guid companyUuid,
            CancellationToken cancellationToken) =>
            Task.FromResult(new CompanyMembership(userUuid, companyUuid, "verified", "OWNER", "active"));

        public Task<CompanyMembership> AuthorizeRoleAsync(
            Guid userUuid,
            Guid companyUuid,
            IReadOnlyCollection<string> allowedRoles,
            CancellationToken cancellationToken) =>
            AuthorizeMembershipAsync(userUuid, companyUuid, cancellationToken);
    }

    private sealed class StubGetCompanyCommandHandler : ICommandHandler<GetCompanyCommandRequest, GetCompanyCommandResponse>
    {
        public Task<GetCompanyCommandResponse> HandleAsync(
            GetCompanyCommandRequest command,
            CancellationToken cancellationToken) =>
            Task.FromResult(new GetCompanyCommandResponse(
                command.CompanyUuid,
                CompanyCode: null,
                "Example Retail Pvt Ltd",
                "Example Retail",
                "29ABCDE1234F1Z5",
                "accounts@example.com",
                "+919876543211",
                "1 Main Road",
                AddressLine2: null,
                "Bengaluru",
                "Karnataka",
                "560001",
                "IN",
                "pending_verification"));
    }
}

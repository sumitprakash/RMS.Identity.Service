using Microsoft.AspNetCore.Mvc;
using RMS.Identity.Service.Api.Shared.Auth;
using RMS.Identity.Service.Domain.Contracts.Companies;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Api.Endpoint.Companies.GetCompany;

[ApiController]
[Route("api/v1/companies/{companyUuid:guid}")]
public sealed class GetCompanyController : ControllerBase
{
    private readonly IAccessTokenUserResolver _accessTokenUserResolver;
    private readonly ICompanyAccessAuthorizer _companyAccessAuthorizer;
    private readonly IDatabaseTransactionExecutor _databaseTransactionExecutor;
    private readonly ICommandHandler<GetCompanyCommandRequest, GetCompanyCommandResponse> _getCompanyCommandHandler;

    public GetCompanyController(
        IAccessTokenUserResolver accessTokenUserResolver,
        ICompanyAccessAuthorizer companyAccessAuthorizer,
        IDatabaseTransactionExecutor databaseTransactionExecutor,
        ICommandHandler<GetCompanyCommandRequest, GetCompanyCommandResponse> getCompanyCommandHandler)
    {
        _accessTokenUserResolver = accessTokenUserResolver;
        _companyAccessAuthorizer = companyAccessAuthorizer;
        _databaseTransactionExecutor = databaseTransactionExecutor;
        _getCompanyCommandHandler = getCompanyCommandHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(CompanyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(Guid companyUuid, CancellationToken cancellationToken)
    {
        var userUuid = _accessTokenUserResolver.ResolveRequiredUserUuid(HttpContext);

        var company = await _databaseTransactionExecutor.ExecuteAsync(
            async transactionCancellationToken =>
            {
                await _companyAccessAuthorizer.AuthorizeMembershipAsync(
                    userUuid,
                    companyUuid,
                    transactionCancellationToken);

                var loadedCompany = await _getCompanyCommandHandler.HandleAsync(
                    new GetCompanyCommandRequest(companyUuid),
                    transactionCancellationToken);

                return loadedCompany;
            },
            cancellationToken);

        return Ok(company.ToResponse());
    }
}

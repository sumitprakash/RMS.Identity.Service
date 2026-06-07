using Microsoft.AspNetCore.Mvc;
using RMS.Identity.Service.Api.Shared.Auth;
using RMS.Identity.Service.Domain.Contracts.Companies;
using RMS.Identity.Service.Infrastructure.Cqrs;

namespace RMS.Identity.Service.Api.Endpoint.Companies.GetCompany;

[ApiController]
[Route("api/v1/companies/{companyUuid:guid}")]
public sealed class GetCompanyController : ControllerBase
{
    private readonly IAccessTokenUserResolver _accessTokenUserResolver;
    private readonly ICompanyAccessAuthorizer _companyAccessAuthorizer;
    private readonly ICommandHandler<GetCompanyCommandRequest, GetCompanyCommandResponse> _getCompanyCommandHandler;

    public GetCompanyController(
        IAccessTokenUserResolver accessTokenUserResolver,
        ICompanyAccessAuthorizer companyAccessAuthorizer,
        ICommandHandler<GetCompanyCommandRequest, GetCompanyCommandResponse> getCompanyCommandHandler)
    {
        _accessTokenUserResolver = accessTokenUserResolver;
        _companyAccessAuthorizer = companyAccessAuthorizer;
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
        var company = await _getCompanyCommandHandler.HandleAsync(
            new GetCompanyCommandRequest(companyUuid),
            cancellationToken);

        await _companyAccessAuthorizer.AuthorizeMembershipAsync(
            userUuid,
            companyUuid,
            cancellationToken);

        return Ok(company.ToResponse());
    }
}

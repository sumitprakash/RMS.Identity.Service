using Microsoft.AspNetCore.Mvc;
using RMS.Identity.Service.Api.Shared.Auth;
using RMS.Identity.Service.Domain.Contracts.Companies;
using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Api.Endpoint.Companies.UpdateCompany;

[ApiController]
[Route("api/v1/companies/{companyUuid:guid}")]
public sealed class UpdateCompanyController : ControllerBase
{
    private static readonly string[] CompanyManagerRoles = ["OWNER", "ADMIN"];

    private readonly IAccessTokenUserResolver _accessTokenUserResolver;
    private readonly ICompanyAccessAuthorizer _companyAccessAuthorizer;
    private readonly ICommandHandler<UpdateCompanyCommandRequest, UpdateCompanyCommandResponse> _updateCompanyCommandHandler;

    public UpdateCompanyController(
        IAccessTokenUserResolver accessTokenUserResolver,
        ICompanyAccessAuthorizer companyAccessAuthorizer,
        ICommandHandler<UpdateCompanyCommandRequest, UpdateCompanyCommandResponse> updateCompanyCommandHandler)
    {
        _accessTokenUserResolver = accessTokenUserResolver;
        _companyAccessAuthorizer = companyAccessAuthorizer;
        _updateCompanyCommandHandler = updateCompanyCommandHandler;
    }

    [HttpPatch]
    [ProducesResponseType(typeof(CompanyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PatchAsync(
        Guid companyUuid,
        UpdateCompanyRequestBody body,
        CancellationToken cancellationToken)
    {
        var actorUserUuid = _accessTokenUserResolver.ResolveRequiredUserUuid(HttpContext);
        await _companyAccessAuthorizer.AuthorizeRoleAsync(
            actorUserUuid,
            companyUuid,
            CompanyManagerRoles,
            cancellationToken);

        var company = await _updateCompanyCommandHandler.HandleAsync(
            body.ToCommand(companyUuid),
            cancellationToken);

        return Ok(company.ToResponse());
    }
}

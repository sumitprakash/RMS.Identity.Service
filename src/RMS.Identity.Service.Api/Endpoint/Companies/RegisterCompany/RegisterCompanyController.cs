using Microsoft.AspNetCore.Mvc;
using RMS.Identity.Service.Api.Shared.Auth;
using RMS.Identity.Service.Domain.Contracts.Companies;
using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Api.Endpoint.Companies.RegisterCompany;

[ApiController]
[Route("api/v1/companies")]
public sealed class RegisterCompanyController : ControllerBase
{
    private readonly IAccessTokenUserResolver _accessTokenUserResolver;
    private readonly ICommandHandler<RegisterCompanyCommandRequest, RegisterCompanyCommandResponse> _registerCompanyCommandHandler;

    public RegisterCompanyController(
        IAccessTokenUserResolver accessTokenUserResolver,
        ICommandHandler<RegisterCompanyCommandRequest, RegisterCompanyCommandResponse> registerCompanyCommandHandler)
    {
        _accessTokenUserResolver = accessTokenUserResolver;
        _registerCompanyCommandHandler = registerCompanyCommandHandler;
    }

    [HttpPost]
    [ServiceFilter(typeof(RegisterCompanyRequestValidationFilter))]
    [ProducesResponseType(typeof(RegisterCompanyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PostAsync(RegisterCompanyRequest request, CancellationToken cancellationToken)
    {
        var ownerUserUuid = _accessTokenUserResolver.ResolveRequiredUserUuid(HttpContext);
        var company = await _registerCompanyCommandHandler.HandleAsync(request.ToCommand(ownerUserUuid), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, company.ToResponse());
    }
}

using Microsoft.AspNetCore.Mvc;
using RMS.Identity.Service.Api.Shared.Auth;
using RMS.Identity.Service.Domain.Contracts.Companies;
using RMS.Identity.Service.Domain.Contracts.CompanyUsers;
using RMS.Identity.Service.Infrastructure.Cqrs;

namespace RMS.Identity.Service.Api.Endpoint.Companies;

[ApiController]
[Route("api/v1/companies")]
public sealed class CompaniesController : ControllerBase
{
    private readonly IAccessTokenUserResolver _accessTokenUserResolver;
    private readonly ICompanyAccessAuthorizer _companyAccessAuthorizer;
    private readonly ICommandHandler<CreateCompanyUserCommandRequest, CreateCompanyUserCommandResponse> _createCompanyUserCommandHandler;
    private readonly ICommandHandler<RegisterCompanyCommandRequest, RegisterCompanyCommandResponse> _registerCompanyCommandHandler;
    private readonly ICommandHandler<GetCurrentUserCompaniesCommandRequest, GetCurrentUserCompaniesCommandResponse> _getCurrentUserCompaniesCommandHandler;

    public CompaniesController(
        IAccessTokenUserResolver accessTokenUserResolver,
        ICompanyAccessAuthorizer companyAccessAuthorizer,
        ICommandHandler<CreateCompanyUserCommandRequest, CreateCompanyUserCommandResponse> createCompanyUserCommandHandler,
        ICommandHandler<RegisterCompanyCommandRequest, RegisterCompanyCommandResponse> registerCompanyCommandHandler,
        ICommandHandler<GetCurrentUserCompaniesCommandRequest, GetCurrentUserCompaniesCommandResponse> getCurrentUserCompaniesCommandHandler)
    {
        _accessTokenUserResolver = accessTokenUserResolver;
        _companyAccessAuthorizer = companyAccessAuthorizer;
        _createCompanyUserCommandHandler = createCompanyUserCommandHandler;
        _registerCompanyCommandHandler = registerCompanyCommandHandler;
        _getCurrentUserCompaniesCommandHandler = getCurrentUserCompaniesCommandHandler;
    }

    [HttpGet("/api/v1/current-user/companies")]
    [ProducesResponseType(typeof(CurrentUserCompaniesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMineAsync(CancellationToken cancellationToken)
    {
        var userUuid = _accessTokenUserResolver.ResolveRequiredUserUuid(HttpContext);
        var companies = await _getCurrentUserCompaniesCommandHandler.HandleAsync(
            new GetCurrentUserCompaniesCommandRequest(userUuid),
            cancellationToken);

        return Ok(companies.ToResponse());
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

    [HttpPost("{companyUuid:guid}/users")]
    [ServiceFilter(typeof(CreateCompanyUserRequestValidationFilter))]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PostUserAsync(
        Guid companyUuid,
        CreateCompanyUserRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserUuid = _accessTokenUserResolver.ResolveRequiredUserUuid(HttpContext);
        await _companyAccessAuthorizer.AuthorizeRoleAsync(
            actorUserUuid,
            companyUuid,
            new[] { "OWNER", "ADMIN" },
            cancellationToken);

        var user = await _createCompanyUserCommandHandler.HandleAsync(
            request.ToCommand(companyUuid),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, user.ToResponse());
    }
}

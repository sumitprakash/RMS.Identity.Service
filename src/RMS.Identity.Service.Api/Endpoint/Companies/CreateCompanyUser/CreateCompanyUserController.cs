using Microsoft.AspNetCore.Mvc;
using RMS.Identity.Service.Api.Shared.Auth;
using RMS.Identity.Service.Domain.Contracts.CompanyUsers;
using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Api.Endpoint.Companies.CreateCompanyUser;

[ApiController]
[Route("api/v1/companies/{companyUuid:guid}/users")]
public sealed class CreateCompanyUserController : ControllerBase
{
    private readonly IAccessTokenUserResolver _accessTokenUserResolver;
    private readonly ICompanyAccessAuthorizer _companyAccessAuthorizer;
    private readonly ICommandHandler<CreateCompanyUserCommandRequest, CreateCompanyUserCommandResponse> _createCompanyUserCommandHandler;

    public CreateCompanyUserController(
        IAccessTokenUserResolver accessTokenUserResolver,
        ICompanyAccessAuthorizer companyAccessAuthorizer,
        ICommandHandler<CreateCompanyUserCommandRequest, CreateCompanyUserCommandResponse> createCompanyUserCommandHandler)
    {
        _accessTokenUserResolver = accessTokenUserResolver;
        _companyAccessAuthorizer = companyAccessAuthorizer;
        _createCompanyUserCommandHandler = createCompanyUserCommandHandler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PostAsync(
        CreateCompanyUserRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserUuid = _accessTokenUserResolver.ResolveRequiredUserUuid(HttpContext);
        await _companyAccessAuthorizer.AuthorizeRoleAsync(
            actorUserUuid,
            request.CompanyUuid,
            new[] { "OWNER", "ADMIN" },
            cancellationToken);

        var user = await _createCompanyUserCommandHandler.HandleAsync(
            request.ToCommand(actorUserUuid),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, user.ToResponse());
    }
}

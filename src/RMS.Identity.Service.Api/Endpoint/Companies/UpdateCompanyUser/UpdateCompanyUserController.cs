using Microsoft.AspNetCore.Mvc;
using RMS.Identity.Service.Api.Shared.Auth;
using RMS.Identity.Service.Domain.Contracts.CompanyUsers;
using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Api.Endpoint.Companies.UpdateCompanyUser;

[ApiController]
[Route("api/v1/companies/{companyUuid:guid}/users/{userUuid:guid}")]
public sealed class UpdateCompanyUserController : ControllerBase
{
    private static readonly string[] CompanyUserManagerRoles = ["OWNER", "ADMIN"];

    private readonly IAccessTokenUserResolver _accessTokenUserResolver;
    private readonly ICompanyAccessAuthorizer _companyAccessAuthorizer;
    private readonly ICommandHandler<SuspendCompanyUserCommandRequest, SuspendCompanyUserCommandResponse> _suspendCompanyUserCommandHandler;
    private readonly ICommandHandler<UpdateCompanyUserCommandRequest, UpdateCompanyUserCommandResponse> _updateCompanyUserCommandHandler;

    public UpdateCompanyUserController(
        IAccessTokenUserResolver accessTokenUserResolver,
        ICompanyAccessAuthorizer companyAccessAuthorizer,
        ICommandHandler<SuspendCompanyUserCommandRequest, SuspendCompanyUserCommandResponse> suspendCompanyUserCommandHandler,
        ICommandHandler<UpdateCompanyUserCommandRequest, UpdateCompanyUserCommandResponse> updateCompanyUserCommandHandler)
    {
        _accessTokenUserResolver = accessTokenUserResolver;
        _companyAccessAuthorizer = companyAccessAuthorizer;
        _suspendCompanyUserCommandHandler = suspendCompanyUserCommandHandler;
        _updateCompanyUserCommandHandler = updateCompanyUserCommandHandler;
    }

    [HttpPatch]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PatchAsync(
        Guid companyUuid,
        Guid userUuid,
        UpdateCompanyUserRequestBody body,
        CancellationToken cancellationToken)
    {
        var actorUserUuid = _accessTokenUserResolver.ResolveRequiredUserUuid(HttpContext);
        await _companyAccessAuthorizer.AuthorizeRoleAsync(
            actorUserUuid,
            companyUuid,
            CompanyUserManagerRoles,
            cancellationToken);

        var response = await _updateCompanyUserCommandHandler.HandleAsync(
            new UpdateCompanyUserCommandRequest(
                actorUserUuid,
                companyUuid,
                userUuid,
                body.CompanyRole!.Value,
                body.MembershipStatus!.Value),
            cancellationToken);

        return Ok(response.ToResponse());
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAsync(
        Guid companyUuid,
        Guid userUuid,
        CancellationToken cancellationToken)
    {
        var actorUserUuid = _accessTokenUserResolver.ResolveRequiredUserUuid(HttpContext);
        await _companyAccessAuthorizer.AuthorizeRoleAsync(
            actorUserUuid,
            companyUuid,
            CompanyUserManagerRoles,
            cancellationToken);

        await _suspendCompanyUserCommandHandler.HandleAsync(
            new SuspendCompanyUserCommandRequest(actorUserUuid, companyUuid, userUuid),
            cancellationToken);

        return NoContent();
    }
}

using Microsoft.AspNetCore.Mvc;
using RMS.Identity.Service.Api.Shared.Auth;
using RMS.Identity.Service.Domain.Contracts.CompanyUsers;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Api.Endpoint.Companies.GetCompanyUser;

[ApiController]
[Route("api/v1/companies/{companyUuid:guid}/users/{userUuid:guid}")]
public sealed class GetCompanyUserController : ControllerBase
{
    private static readonly string[] CompanyUserReaderRoles = ["OWNER", "ADMIN"];

    private readonly IAccessTokenUserResolver _accessTokenUserResolver;
    private readonly ICompanyAccessAuthorizer _companyAccessAuthorizer;
    private readonly IDatabaseTransactionExecutor _databaseTransactionExecutor;
    private readonly ICommandHandler<GetCompanyUserCommandRequest, GetCompanyUserCommandResponse> _getCompanyUserCommandHandler;

    public GetCompanyUserController(
        IAccessTokenUserResolver accessTokenUserResolver,
        ICompanyAccessAuthorizer companyAccessAuthorizer,
        IDatabaseTransactionExecutor databaseTransactionExecutor,
        ICommandHandler<GetCompanyUserCommandRequest, GetCompanyUserCommandResponse> getCompanyUserCommandHandler)
    {
        _accessTokenUserResolver = accessTokenUserResolver;
        _companyAccessAuthorizer = companyAccessAuthorizer;
        _databaseTransactionExecutor = databaseTransactionExecutor;
        _getCompanyUserCommandHandler = getCompanyUserCommandHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(
        Guid companyUuid,
        Guid userUuid,
        CancellationToken cancellationToken)
    {
        var actorUserUuid = _accessTokenUserResolver.ResolveRequiredUserUuid(HttpContext);

        var user = await _databaseTransactionExecutor.ExecuteAsync(
            async transactionCancellationToken =>
            {
                var loadedUser = await _getCompanyUserCommandHandler.HandleAsync(
                    new GetCompanyUserCommandRequest(companyUuid, userUuid),
                    transactionCancellationToken);

                if (actorUserUuid == userUuid)
                {
                    await _companyAccessAuthorizer.AuthorizeMembershipAsync(
                        actorUserUuid,
                        companyUuid,
                        transactionCancellationToken);
                }
                else
                {
                    await _companyAccessAuthorizer.AuthorizeRoleAsync(
                        actorUserUuid,
                        companyUuid,
                        CompanyUserReaderRoles,
                        transactionCancellationToken);
                }

                return loadedUser;
            },
            cancellationToken);

        return Ok(user.ToResponse());
    }
}
